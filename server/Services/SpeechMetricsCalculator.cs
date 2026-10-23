using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using EmployeeHRMS.Api.Configuration;
using EmployeeHRMS.Api.Models.ValueObjects;

namespace EmployeeHRMS.Api.Services
{
    public class WordTimestamp
    {
        public string Word { get; set; } = string.Empty;
        public double Start { get; set; }
        public double End { get; set; }
    }

    public class SegmentData
    {
        public int Id { get; set; }
        public double Start { get; set; }
        public double End { get; set; }
        public string Text { get; set; } = string.Empty;
        public double? NoSpeechProbability { get; set; }
    }

    /// <summary>
    /// Pure function calculator: Tính toán các chỉ số nhịp nói (WPM, độ biến thiên khoảng lặng CV)
    /// và phát hiện các mẫu im lặng / ảo giác.
    /// Không chứa I/O, không gọi DB hay LLM.
    /// </summary>
    public static class SpeechMetricsCalculator
    {
        private static readonly Regex PunctuationRegex = new(@"[^\w\s]", RegexOptions.Compiled);
        private static readonly Regex MultipleWhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

        /// <summary>
        /// Parse JSON từ cột TranscriptSegments (do Whisper API verbose_json sinh ra)
        /// thành danh sách từ có timestamp và các segment.
        /// </summary>
        public static (List<WordTimestamp> Words, List<SegmentData> Segments) ParseSegmentsJson(string? segmentsJson)
        {
            var words = new List<WordTimestamp>();
            var segments = new List<SegmentData>();

            if (string.IsNullOrWhiteSpace(segmentsJson))
                return (words, segments);

            try
            {
                using var doc = JsonDocument.Parse(segmentsJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("words", out var wordsElem) && wordsElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var w in wordsElem.EnumerateArray())
                    {
                        var wordText = w.TryGetProperty("word", out var wt) ? wt.GetString() ?? "" : "";
                        var start = w.TryGetProperty("start", out var ws) && ws.TryGetDouble(out var sVal) ? sVal : 0;
                        var end = w.TryGetProperty("end", out var we) && we.TryGetDouble(out var eVal) ? eVal : 0;

                        if (!string.IsNullOrWhiteSpace(wordText))
                        {
                            words.Add(new WordTimestamp
                            {
                                Word = wordText.Trim(),
                                Start = start,
                                End = end
                            });
                        }
                    }
                }

                if (root.TryGetProperty("segments", out var segmentsElem) && segmentsElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var s in segmentsElem.EnumerateArray())
                    {
                        var id = s.TryGetProperty("id", out var sid) && sid.TryGetInt32(out var idVal) ? idVal : 0;
                        var start = s.TryGetProperty("start", out var ss) && ss.TryGetDouble(out var sVal) ? sVal : 0;
                        var end = s.TryGetProperty("end", out var se) && se.TryGetDouble(out var eVal) ? eVal : 0;
                        var text = s.TryGetProperty("text", out var st) ? st.GetString() ?? "" : "";

                        double? noSpeechProb = null;
                        if (s.TryGetProperty("noSpeechProbability", out var nsp) && nsp.TryGetDouble(out var nspVal))
                        {
                            noSpeechProb = nspVal;
                        }
                        else if (s.TryGetProperty("no_speech_prob", out var nsp2) && nsp2.TryGetDouble(out var nspVal2))
                        {
                            noSpeechProb = nspVal2;
                        }

                        segments.Add(new SegmentData
                        {
                            Id = id,
                            Start = start,
                            End = end,
                            Text = text.Trim(),
                            NoSpeechProbability = noSpeechProb
                        });
                    }
                }
            }
            catch
            {
                // Json hỏng: trả về danh sách rỗng an toàn, không ném exception
            }

            return (words, segments);
        }

        /// <summary>
        /// Tính toán các chỉ số nhịp nói (WPM, độ biến thiên khoảng nghỉ CV, cờ chỉ báo nhịp đều bất thường).
        /// </summary>
        public static SpeechMetrics Calculate(
            List<WordTimestamp>? words,
            double? sttDurationSeconds,
            SpeechMetricsOptions options)
        {
            options ??= new SpeechMetricsOptions();

            if (words == null || words.Count == 0)
            {
                return new SpeechMetrics
                {
                    Wpm = 0,
                    SpeechDurationSeconds = 0,
                    WordCount = 0,
                    PauseVariationCv = 0,
                    AveragePauseSeconds = 0,
                    PauseCount = 0,
                    IsRhythmMonotonous = false,
                    CalculatedAt = DateTime.UtcNow
                };
            }

            // Sắp xếp các từ theo timestamp bắt đầu
            var sortedWords = words.OrderBy(w => w.Start).ToList();
            int wordCount = sortedWords.Count;

            // Xác định thời lượng lời nói thực tế: từ đầu tiên đến từ cuối cùng
            var firstWord = sortedWords[0];
            var lastWord = sortedWords[^1];

            double rawSpeechDuration = Math.Max(0, lastWord.End - firstWord.Start);

            // Fallback nếu duration quá nhỏ hoặc bất thường
            if (rawSpeechDuration <= 0.1 && sttDurationSeconds.HasValue && sttDurationSeconds.Value > 0)
            {
                rawSpeechDuration = sttDurationSeconds.Value;
            }

            double speechDurationSeconds = Math.Max(0.1, rawSpeechDuration);

            // WPM = (WordCount / DurationMinutes)
            double wpm = Math.Round((wordCount / (speechDurationSeconds / 60.0)), 1);

            // Trích xuất các khoảng lặng (pauses) giữa các từ kế tiếp nhau
            var pauses = new List<double>();
            for (int i = 0; i < sortedWords.Count - 1; i++)
            {
                double pause = sortedWords[i + 1].Start - sortedWords[i].End;
                // Chỉ nhận khoảng lặng có ý nghĩa (bỏ qua âm tiết nối hoặc overlap âm)
                if (pause >= 0.05)
                {
                    pauses.Add(pause);
                }
            }

            int pauseCount = pauses.Count;
            double averagePauseSeconds = 0;
            double pauseVariationCv = 0;

            if (pauseCount > 0)
            {
                averagePauseSeconds = Math.Round(pauses.Average(), 3);

                // Sample standard deviation nếu N >= 2, population standard deviation nếu N == 1
                if (pauseCount >= 2 && averagePauseSeconds > 0)
                {
                    double sumOfSquares = pauses.Sum(p => Math.Pow(p - averagePauseSeconds, 2));
                    double stdDev = Math.Sqrt(sumOfSquares / (pauseCount - 1));
                    pauseVariationCv = Math.Round(stdDev / averagePauseSeconds, 3);
                }
                else
                {
                    pauseVariationCv = 0;
                }
            }

            // Gắn cờ chỉ báo nhịp đọc đều bất thường:
            // Cần tối thiểu 5 khoảng lặng để đảm bảo ý nghĩa thống kê cho mẫu nhỏ
            bool isRhythmMonotonous = pauseCount >= 5 && pauseVariationCv < options.PauseVariationCvThreshold;

            return new SpeechMetrics
            {
                Wpm = wpm,
                SpeechDurationSeconds = Math.Round(speechDurationSeconds, 2),
                WordCount = wordCount,
                PauseVariationCv = pauseVariationCv,
                AveragePauseSeconds = averagePauseSeconds,
                PauseCount = pauseCount,
                IsRhythmMonotonous = isRhythmMonotonous,
                CalculatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Chuẩn hóa chuỗi văn bản: chữ thường, xóa dấu câu, thu gọn khoảng trắng.
        /// </summary>
        public static string NormalizeText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var lower = text.ToLowerInvariant();
            var noPunctuation = PunctuationRegex.Replace(lower, " ");
            var collapsed = MultipleWhitespaceRegex.Replace(noPunctuation, " ").Trim();
            return collapsed;
        }

        /// <summary>
        /// Kiểm tra xem transcript có chỉ gồm các cụm từ ảo giác khi người dùng im lặng hay không (D2).
        /// </summary>
        public static bool IsKnownHallucination(string? transcript, SpeechMetricsOptions options)
        {
            if (string.IsNullOrWhiteSpace(transcript))
                return false;

            var normalizedTranscript = NormalizeText(transcript);
            if (string.IsNullOrWhiteSpace(normalizedTranscript))
                return false;

            var phrases = options?.KnownHallucinationPhrases ?? new List<string>();

            // 1. So khớp trực tiếp toàn bộ chuỗi
            foreach (var phrase in phrases)
            {
                var normalizedPhrase = NormalizeText(phrase);
                if (string.Equals(normalizedTranscript, normalizedPhrase, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            // 2. Kiểm tra nếu loại bỏ toàn bộ các cụm ảo giác thì chuỗi còn lại rỗng hoặc vô nghĩa (< 3 ký tự)
            var remaining = normalizedTranscript;
            foreach (var phrase in phrases)
            {
                var normalizedPhrase = NormalizeText(phrase);
                if (!string.IsNullOrWhiteSpace(normalizedPhrase))
                {
                    remaining = remaining.Replace(normalizedPhrase, "").Trim();
                }
            }

            return remaining.Length == 0;
        }

        /// <summary>
        /// Kiểm tra xem toàn bộ các segment có xác suất không có tiếng nói (no_speech_prob) vượt ngưỡng im lặng hay không.
        /// </summary>
        public static bool IsSilentSegments(List<SegmentData>? segments, SpeechMetricsOptions options)
        {
            if (segments == null || segments.Count == 0)
                return false;

            double threshold = options?.MaxNoSpeechProbability ?? 0.8;

            return segments.All(s => s.NoSpeechProbability.HasValue && s.NoSpeechProbability.Value >= threshold);
        }
    }
}

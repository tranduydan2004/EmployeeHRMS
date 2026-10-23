using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EmployeeHRMS.Api.Services
{
    public enum InjectionRiskLevel
    {
        None = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }

    /// <summary>
    /// Kết quả quét phát hiện dấu hiệu can thiệp prompt (Prompt Injection) từ transcript ứng viên.
    /// Thông tin này hoàn toàn là telemetry trung lập cho HR tham khảo, TUYỆT ĐỐI không tự động trừ điểm hay đánh rớt ứng viên.
    /// </summary>
    public class PromptInjectionScanResult
    {
        public bool IsSuspicious => DetectedPatterns.Count > 0;
        public InjectionRiskLevel RiskLevel { get; set; } = InjectionRiskLevel.None;
        public List<string> DetectedPatterns { get; set; } = new();
        public string? TelemetrySummary { get; set; }
    }

    /// <summary>
    /// Pure function scanner: Phát hiện các mẫu câu cố tình can thiệp prompt / jailbreak trong transcript
    /// (tiếng Anh và tiếng Việt), bao gồm: chỉ thị ghi đè, giả mạo vai trò hệ thống, ép điểm tuyệt đối,
    /// và thoát khỏi thẻ bao bọc (delimiters).
    /// </summary>
    public static class PromptInjectionScanner
    {
        // 1. Thẻ đánh dấu cấu trúc (Delimiter breakouts) - High risk
        private static readonly string[] DelimiterPatterns = new[]
        {
            "</transcript>", "<transcript>",
            "<system>", "</system>",
            "<prompt>", "</prompt>",
            "```json", "```",
            "[system]", "[/system]",
            "assistant:", "system:"
        };

        // 2. Chỉ thị ghi đè prompt / jailbreak (System overrides) - High risk
        private static readonly Regex[] HighRiskRegexes = new[]
        {
            new Regex(@"\b(ignore|disregard|forget)\s+(all\s+)?(previous|above|prior)\s+(instructions|prompts|rules)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(50)),
            new Regex(@"(bỏ\s+qua|quên)\s+(mọi\s+|tất\s+cả\s+)?(hướng\s+dẫn|chỉ\s+thị|quy\s+tắc)\s+(trước|ở\s+trên)", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(50)),
            new Regex(@"\byou\s+are\s+now\s+(an?\s+)?(helpful|ai|assistant|evaluator|rater)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(50)),
            new Regex(@"(từ\s+bây\s+giờ\s+bạn\s+là|hãy\s+đóng\s+vai\s+như|hãy\s+giả\s+vờ\s+là)", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(50))
        };

        // 3. Mệnh lệnh ép điểm / thao túng kết quả (Forced scoring commands) - Medium risk
        private static readonly Regex[] MediumRiskRegexes = new[]
        {
            new Regex(@"\b(give|assign|award|rate)\s+((\w+\s+){0,3})?(a\s+)?(score\s+of\s+|score\s+)?(10(\.0)?(\s*\/\s*10)?|100%|full\s+marks?|excellent)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(50)),
            new Regex(@"(cho|chấm|đánh\s+giá)\s+(tôi\s+)?(10(\s*\/\s*10)?\s*điểm|điểm\s+tối\s+đa|xuất\s+sắc|level\s*4)", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(50)),
            new Regex(@"\boutput\s+only\s+(json|valid\s+json|excellent)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(50))
        };

        /// <summary>
        /// Quét nội dung văn bản transcript để tìm các mẫu can thiệp prompt.
        /// </summary>
        public static PromptInjectionScanResult Scan(string? transcript)
        {
            var result = new PromptInjectionScanResult();

            if (string.IsNullOrWhiteSpace(transcript))
            {
                return result;
            }

            // 1. Quét Delimiter breakouts
            foreach (var delimiter in DelimiterPatterns)
            {
                if (transcript.IndexOf(delimiter, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result.DetectedPatterns.Add($"Phát hiện ký tự phân tách/thẻ thoát: '{delimiter}'");
                }
            }

            // 2. Quét High risk regexes (Jailbreak / System override)
            foreach (var regex in HighRiskRegexes)
            {
                try
                {
                    var match = regex.Match(transcript);
                    if (match.Success)
                    {
                        result.DetectedPatterns.Add($"Phát hiện chỉ thị ghi đè prompt: '{match.Value}'");
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    // Timeout an toàn khi regex chạy quá 50ms
                }
            }

            // 3. Quét Medium risk regexes (Scoring manipulation)
            foreach (var regex in MediumRiskRegexes)
            {
                try
                {
                    var match = regex.Match(transcript);
                    if (match.Success)
                    {
                        result.DetectedPatterns.Add($"Phát hiện lệnh thao túng điểm: '{match.Value}'");
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    // Timeout an toàn
                }
            }

            // Đánh giá mức độ rủi ro (Risk Level)
            if (result.DetectedPatterns.Count > 0)
            {
                bool hasHighRisk = result.DetectedPatterns.Any(p => p.Contains("thẻ thoát") || p.Contains("ghi đè"));
                result.RiskLevel = hasHighRisk ? InjectionRiskLevel.High : InjectionRiskLevel.Medium;
                result.TelemetrySummary = $"Cảnh báo can thiệp prompt ({result.RiskLevel}): {string.Join("; ", result.DetectedPatterns)}";
            }

            return result;
        }
    }
}

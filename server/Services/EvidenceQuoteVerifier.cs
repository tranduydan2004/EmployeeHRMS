using System;
using System.Collections.Generic;
using System.Linq;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Kết quả xác thực các trích dẫn bằng chứng (evidence quotes) do LLM trả về so với transcript gốc.
    /// </summary>
    public class EvidenceVerificationResult
    {
        public bool IsValid => UnverifiedQuotes.Count == 0;
        public int TotalQuotes { get; set; }
        public int VerifiedQuotesCount => VerifiedQuotes.Count;
        public int UnverifiedQuotesCount => UnverifiedQuotes.Count;
        public double VerificationRatio { get; set; }
        public List<string> VerifiedQuotes { get; set; } = new();
        public List<string> UnverifiedQuotes { get; set; } = new();
    }

    /// <summary>
    /// Pure function verifier: Xác minh các trích dẫn bằng chứng (evidence quotes) do LLM viện dẫn
    /// có thực sự tồn tại trong transcript gốc của ứng viên hay không, nhằm phát hiện ảo giác (hallucination).
    /// </summary>
    public static class EvidenceQuoteVerifier
    {
        /// <summary>
        /// Xác minh danh sách trích dẫn so với transcriptSnapshot.
        /// Sử dụng chuẩn hóa ký tự (chữ thường, xóa dấu câu, gộp khoảng trắng) để tránh sai lệch nhỏ do định dạng.
        /// </summary>
        public static EvidenceVerificationResult Verify(IEnumerable<string>? quotes, string? transcriptSnapshot)
        {
            var result = new EvidenceVerificationResult();

            var rawQuotes = quotes?
                .Where(q => !string.IsNullOrWhiteSpace(q))
                .Select(q => q.Trim())
                .ToList() ?? new List<string>();

            result.TotalQuotes = rawQuotes.Count;

            if (result.TotalQuotes == 0)
            {
                result.VerificationRatio = 1.0;
                return result;
            }

            if (string.IsNullOrWhiteSpace(transcriptSnapshot))
            {
                // Có trích dẫn nhưng transcript rỗng -> toàn bộ trích dẫn là ảo giác
                result.UnverifiedQuotes.AddRange(rawQuotes);
                result.VerificationRatio = 0.0;
                return result;
            }

            // Chuẩn hóa transcript: lowercase, không dấu câu, thu gọn space
            var normalizedTranscript = SpeechMetricsCalculator.NormalizeText(transcriptSnapshot);

            foreach (var quote in rawQuotes)
            {
                var normalizedQuote = SpeechMetricsCalculator.NormalizeText(quote);

                if (string.IsNullOrWhiteSpace(normalizedQuote))
                {
                    result.UnverifiedQuotes.Add(quote);
                    continue;
                }

                // 1. Kiểm tra trực tiếp trên transcript gốc (case-insensitive)
                bool existsInRaw = transcriptSnapshot.Contains(quote, StringComparison.OrdinalIgnoreCase);

                // 2. Kiểm tra trên transcript đã chuẩn hóa (chống lỗi khác biệt dấu câu, xuống dòng)
                bool existsInNormalized = normalizedTranscript.Contains(normalizedQuote, StringComparison.Ordinal);

                if (existsInRaw || existsInNormalized)
                {
                    result.VerifiedQuotes.Add(quote);
                }
                else
                {
                    result.UnverifiedQuotes.Add(quote);
                }
            }

            result.VerificationRatio = Math.Round((double)result.VerifiedQuotes.Count / result.TotalQuotes, 2);
            return result;
        }
    }
}

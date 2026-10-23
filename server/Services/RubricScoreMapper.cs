using System;
using EmployeeHRMS.Api.Configuration;
using EmployeeHRMS.Api.Models;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Pure function mapper: Chuyển đổi hai chiều giữa RubricLevel (Poor, Acceptable, Good, Excellent)
    /// và thang điểm số chuẩn 10 (2.5, 5.0, 7.5, 10.0 hoặc theo ScoringOptions tùy biến),
    /// đồng thời phân tích chuỗi từ LLM output.
    /// Không chứa I/O hay side-effects.
    /// </summary>
    public static class RubricScoreMapper
    {
        public const decimal PoorScore = 2.5m;
        public const decimal AcceptableScore = 5.0m;
        public const decimal GoodScore = 7.5m;
        public const decimal ExcellentScore = 10.0m;

        /// <summary>
        /// Quy đổi RubricLevel sang điểm số chuẩn thang 10, tùy biến qua ScoringOptions nếu được cung cấp.
        /// </summary>
        public static decimal ToScore(RubricLevel level, ScoringOptions? options = null)
        {
            decimal poor = options?.PoorScore ?? PoorScore;
            decimal acceptable = options?.AcceptableScore ?? AcceptableScore;
            decimal good = options?.GoodScore ?? GoodScore;
            decimal excellent = options?.ExcellentScore ?? ExcellentScore;

            return level switch
            {
                RubricLevel.Poor => poor,
                RubricLevel.Acceptable => acceptable,
                RubricLevel.Good => good,
                RubricLevel.Excellent => excellent,
                _ => throw new ArgumentOutOfRangeException(nameof(level), $"Unsupported rubric level: {level}")
            };
        }

        /// <summary>
        /// Quy đổi điểm số chuẩn thang 10 về mức RubricLevel gần nhất theo các ranh giới trung vị động,
        /// tùy biến qua ScoringOptions nếu được cung cấp.
        /// </summary>
        public static RubricLevel FromScore(decimal score, ScoringOptions? options = null)
        {
            decimal poor = options?.PoorScore ?? PoorScore;
            decimal acceptable = options?.AcceptableScore ?? AcceptableScore;
            decimal good = options?.GoodScore ?? GoodScore;
            decimal excellent = options?.ExcellentScore ?? ExcellentScore;

            // Tính các mốc ranh giới trung vị (midpoints) giữa các mức
            decimal poorToAcceptable = (poor + acceptable) / 2m;
            decimal acceptableToGood = (acceptable + good) / 2m;
            decimal goodToExcellent = (good + excellent) / 2m;

            if (score >= goodToExcellent) return RubricLevel.Excellent;
            if (score >= acceptableToGood) return RubricLevel.Good;
            if (score >= poorToAcceptable) return RubricLevel.Acceptable;
            return RubricLevel.Poor;
        }

        /// <summary>
        /// Phân tích chuỗi đầu ra từ LLM hoặc người dùng sang RubricLevel.
        /// Hỗ trợ cả tiếng Anh lẫn tiếng Việt và các bí danh phổ biến.
        /// Ném ArgumentException nếu chuỗi không hợp lệ.
        /// </summary>
        public static RubricLevel Parse(string? input)
        {
            if (TryParse(input, out var level))
            {
                return level;
            }

            throw new ArgumentException($"Cannot parse '{input}' to a valid {nameof(RubricLevel)}.", nameof(input));
        }

        /// <summary>
        /// Thử phân tích chuỗi đầu vào sang RubricLevel mà không ném exception.
        /// </summary>
        public static bool TryParse(string? input, out RubricLevel level)
        {
            level = RubricLevel.Poor;
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            var normalized = input.Trim().ToLowerInvariant();

            // Excellent (Level 4)
            if (normalized is "excellent" or "exceptional" or "xuat sac" or "xuất sắc"
                or "level 4" or "level4" or "4" or "4/4" or "top tier")
            {
                level = RubricLevel.Excellent;
                return true;
            }

            // Good (Level 3)
            if (normalized is "good" or "exceeds" or "exceeds expectations" or "gioi" or "giỏi"
                or "tot" or "tốt" or "kha" or "khá" or "level 3" or "level3" or "3" or "3/4")
            {
                level = RubricLevel.Good;
                return true;
            }

            // Acceptable (Level 2)
            if (normalized is "acceptable" or "meets" or "meets expectations" or "dat" or "đạt"
                or "trung binh" or "trung bình" or "pass" or "average" or "level 2" or "level2" or "2" or "2/4")
            {
                level = RubricLevel.Acceptable;
                return true;
            }

            // Poor (Level 1)
            if (normalized is "poor" or "needs improvement" or "kem" or "kém" or "yeu" or "yếu"
                or "chua dat" or "chưa đạt" or "fail" or "unsatisfactory" or "level 1" or "level1" or "1" or "1/4")
            {
                level = RubricLevel.Poor;
                return true;
            }

            // Thử parse enum chuẩn nếu chuỗi khớp với tên enum
            if (Enum.TryParse<RubricLevel>(input.Trim(), ignoreCase: true, out var enumResult) &&
                Enum.IsDefined(typeof(RubricLevel), enumResult))
            {
                level = enumResult;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Lấy tên mô tả mức đánh giá thân thiện cho giao diện người dùng.
        /// </summary>
        public static string GetDisplayName(RubricLevel level, bool vietnamese = true) => level switch
        {
            RubricLevel.Poor => vietnamese ? "Chưa đạt (Poor)" : "Poor",
            RubricLevel.Acceptable => vietnamese ? "Đạt (Acceptable)" : "Acceptable",
            RubricLevel.Good => vietnamese ? "Khá / Tốt (Good)" : "Good",
            RubricLevel.Excellent => vietnamese ? "Xuất sắc (Excellent)" : "Excellent",
            _ => level.ToString()
        };
    }
}

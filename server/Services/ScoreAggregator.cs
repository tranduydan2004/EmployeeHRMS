using System;
using System.Collections.Generic;
using System.Linq;
using EmployeeHRMS.Api.Configuration;
using EmployeeHRMS.Api.Models;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Dữ liệu đầu vào của một câu hỏi đã qua đánh giá để đưa vào tổng hợp điểm.
    /// </summary>
    public class QuestionEvaluationRecord
    {
        public int QuestionId { get; set; }
        public int OrderIndex { get; set; }
        public QuestionCategory? Category { get; set; }
        public EvaluationOutcome Outcome { get; set; }
        public decimal? Score { get; set; }
        public RubricLevel? Level { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Kết quả tổng hợp điểm toàn bộ buổi phỏng vấn và đề xuất gợi ý tuyển dụng.
    /// </summary>
    public class AggregatedEvaluationResult
    {
        public bool IsFullyEvaluated { get; set; }
        public int TotalQuestions { get; set; }
        public int EvaluatedQuestionsCount { get; set; }
        public decimal? OverallScore { get; set; }
        public InterviewRecommendation? Recommendation { get; set; }
        public bool HasTechnicalKnockout { get; set; }
        public List<string> KnockoutReasons { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }

    /// <summary>
    /// Pure function aggregator: Deterministic Rule Engine tổng hợp điểm toàn buổi phỏng vấn
    /// và sinh đề xuất Pass/Consider/Reject theo ngưỡng cấu hình và quy tắc cổng chặn (Knockout gates).
    /// Tuyệt đối 100% tất định (deterministic), không gọi LLM, không có I/O hay side-effects.
    /// </summary>
    public static class ScoreAggregator
    {
        /// <summary>
        /// Tổng hợp điểm số cho buổi phỏng vấn dựa trên danh sách các câu đã đánh giá.
        /// </summary>
        public static AggregatedEvaluationResult Aggregate(
            IReadOnlyCollection<QuestionEvaluationRecord>? evaluatedQuestions,
            int totalQuestionsCount,
            ScoringOptions? options = null)
        {
            options ??= new ScoringOptions();
            var result = new AggregatedEvaluationResult
            {
                TotalQuestions = totalQuestionsCount,
                EvaluatedQuestionsCount = evaluatedQuestions?.Count ?? 0
            };

            // 1. Kiểm tra tính toàn vẹn (Survivor Bias / Missing Data Prevention)
            if (evaluatedQuestions == null || totalQuestionsCount <= 0 || evaluatedQuestions.Count < totalQuestionsCount)
            {
                result.IsFullyEvaluated = false;
                result.OverallScore = null;
                result.Recommendation = null;
                result.Summary = $"Chưa thể tổng hợp: Mới hoàn thành {result.EvaluatedQuestionsCount}/{totalQuestionsCount} câu hỏi.";
                return result;
            }

            // 2. Kiểm tra nếu có bất kỳ câu nào gặp lỗi kỹ thuật (Failed)
            var failedQuestions = evaluatedQuestions.Where(q => q.Outcome == EvaluationOutcome.Failed).ToList();
            if (failedQuestions.Count > 0)
            {
                result.IsFullyEvaluated = false;
                result.OverallScore = null;
                result.Recommendation = null;
                var failedIds = string.Join(", ", failedQuestions.Select(q => $"#{q.QuestionId}"));
                result.Summary = $"Chưa thể tổng hợp: Câu hỏi {failedIds} gặp lỗi kỹ thuật trong quá trình chấm điểm. Cần chạy lại.";
                return result;
            }

            // 3. Kiểm tra nếu có câu không thể chấm điểm (NotScorable: MissingRubric, v.v.)
            var notScorableQuestions = evaluatedQuestions.Where(q => q.Outcome == EvaluationOutcome.NotScorable).ToList();
            if (notScorableQuestions.Count > 0)
            {
                result.IsFullyEvaluated = false;
                result.OverallScore = null;
                result.Recommendation = null;
                var reasons = string.Join("; ", notScorableQuestions.Select(q => $"Câu #{q.QuestionId}: {q.Reason ?? "Không có barem rubric"}"));
                result.Summary = $"Chưa thể tổng hợp điểm tự động: {reasons}. Cần HR xem xét thủ công.";
                return result;
            }

            // 4. Trích xuất điểm số hợp lệ cho từng câu:
            // - Succeeded: lấy Score thực tế (hoặc quy đổi từ Level)
            // - NoResponse (Im lặng / Ảo giác): tính 0.0 điểm
            var effectiveScores = new List<(QuestionEvaluationRecord Question, decimal Score)>();

            foreach (var q in evaluatedQuestions)
            {
                if (q.Outcome == EvaluationOutcome.NoResponse)
                {
                    effectiveScores.Add((q, 0.0m));
                }
                else if (q.Outcome == EvaluationOutcome.Succeeded)
                {
                    decimal score = q.Score ?? (q.Level.HasValue ? RubricScoreMapper.ToScore(q.Level.Value, options) : 0.0m);
                    effectiveScores.Add((q, score));
                }
            }

            if (effectiveScores.Count != totalQuestionsCount)
            {
                result.IsFullyEvaluated = false;
                result.OverallScore = null;
                result.Recommendation = null;
                result.Summary = "Dữ liệu điểm không đầy đủ để tổng hợp.";
                return result;
            }

            // 5. Tính điểm trung bình toàn bài
            decimal rawAverage = effectiveScores.Average(x => x.Score);
            decimal overallScore = Math.Round(rawAverage, 2, MidpointRounding.AwayFromZero);
            result.OverallScore = overallScore;
            result.IsFullyEvaluated = true;

            // 6. Kiểm tra Knockout rule trên các câu hỏi kỹ thuật (Category == Technical)
            foreach (var (q, score) in effectiveScores)
            {
                if (q.Category == QuestionCategory.Technical)
                {
                    if (score < options.TechnicalKnockoutMinScore)
                    {
                        result.HasTechnicalKnockout = true;
                        result.KnockoutReasons.Add(
                            $"Câu hỏi kỹ thuật #{q.QuestionId} đạt {score:F1}/10.0 (dưới ngưỡng chặn {options.TechnicalKnockoutMinScore:F1}/10.0).");
                    }
                }
            }

            // 7. Xác định đề xuất Pass / Consider / Reject
            if (result.HasTechnicalKnockout)
            {
                // Bị knockout kỹ thuật: không thể Pass dù điểm tổng có >= 7.0
                if (overallScore >= options.ConsiderThreshold)
                {
                    result.Recommendation = InterviewRecommendation.Consider;
                    result.Summary = overallScore >= options.PassThreshold
                        ? $"Đề xuất: CONSIDER (Điểm TB {overallScore:F1}/10 đạt ngưỡng Pass nhưng bị hạ xuống Consider do không đạt yêu cầu ở câu hỏi kỹ thuật cốt lõi)."
                        : $"Đề xuất: CONSIDER (Điểm TB {overallScore:F1}/10, có cảnh báo knockout kỹ thuật).";
                }
                else
                {
                    result.Recommendation = InterviewRecommendation.Reject;
                    result.Summary = $"Đề xuất: REJECT (Điểm TB {overallScore:F1}/10 dưới ngưỡng {options.ConsiderThreshold:F1} và vi phạm knockout kỹ thuật).";
                }
            }
            else
            {
                // Không vi phạm knockout
                if (overallScore >= options.PassThreshold)
                {
                    result.Recommendation = InterviewRecommendation.Pass;
                    result.Summary = $"Đề xuất: PASS (Điểm TB {overallScore:F1}/10 đạt ngưỡng >= {options.PassThreshold:F1}/10 trên toàn bộ {totalQuestionsCount}/{totalQuestionsCount} câu hỏi).";
                }
                else if (overallScore >= options.ConsiderThreshold)
                {
                    result.Recommendation = InterviewRecommendation.Consider;
                    result.Summary = $"Đề xuất: CONSIDER (Điểm TB {overallScore:F1}/10 trong vùng xem xét {options.ConsiderThreshold:F1} - {options.PassThreshold:F1}).";
                }
                else
                {
                    result.Recommendation = InterviewRecommendation.Reject;
                    result.Summary = $"Đề xuất: REJECT (Điểm TB {overallScore:F1}/10 dưới ngưỡng tối thiểu {options.ConsiderThreshold:F1}).";
                }
            }

            // 8. Tạo chi tiết chiết tính
            var detailItems = effectiveScores.Select(x =>
                $"- Câu #{x.Question.QuestionId} [{x.Question.Category?.ToString() ?? "General"}]: {x.Score:F1}/10.0 ({x.Question.Outcome})");
            result.Details = string.Join(Environment.NewLine, detailItems);

            return result;
        }
    }
}

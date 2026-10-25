using System;
using System.Collections.Generic;
using EmployeeHRMS.Api.Exceptions;
using EmployeeHRMS.Api.Models.ValueObjects;

namespace EmployeeHRMS.Api.Models
{
    public enum ScreeningDecisionType
    {
        AdvanceToInDepth = 1,
        Reject = 2,
        HoldForReview = 3
    }

    /// <summary>
    /// Bảng quyết định bất biến (Append-only audit trail) ghi nhận quyết định sơ loại của HR/Interviewer.
    /// Lưu trữ snapshot gợi ý sơ bộ của hệ thống và đánh giá từng câu tại thời điểm ra quyết định.
    /// Không cho phép sửa hay xóa bản ghi đã tạo.
    /// </summary>
    public class ScreeningDecision
    {
        public int Id { get; set; }

        public int ApplicationId { get; set; }
        public Application Application { get; set; } = null!;

        public int InterviewId { get; set; }
        public Interview Interview { get; set; } = null!;

        public int DecidedByUserId { get; set; }
        public ApplicationUser DecidedByUser { get; set; } = null!;

        public string DecidedByRole { get; set; } = string.Empty;
        public DateTime DecidedAt { get; private set; } = DateTime.UtcNow;

        public ScreeningDecisionType DecisionType { get; set; }

        public string SystemRecommendation { get; set; } = string.Empty;
        public decimal? SystemOverallScore { get; set; }

        public List<QuestionEvaluationSnapshotItem> EvaluationSnapshot { get; set; } = new();

        public int ScoredQuestionCount { get; set; }
        public int TotalQuestionCount { get; set; }

        public string? OverrideReason { get; set; }
        public string? Notes { get; set; }

        // Parameterless constructor for EF Core
        public ScreeningDecision() { }

        public static ScreeningDecision Create(
            int applicationId,
            int interviewId,
            int decidedByUserId,
            string decidedByRole,
            ScreeningDecisionType decisionType,
            string systemRecommendation,
            decimal? systemOverallScore,
            List<QuestionEvaluationSnapshotItem> evaluationSnapshot,
            int scoredQuestionCount,
            int totalQuestionCount,
            string? overrideReason,
            string? notes)
        {
            var isOverriding = (systemRecommendation.Equals("Pass", StringComparison.OrdinalIgnoreCase) && decisionType == ScreeningDecisionType.Reject) ||
                               (systemRecommendation.Equals("Reject", StringComparison.OrdinalIgnoreCase) && decisionType == ScreeningDecisionType.AdvanceToInDepth);

            if (isOverriding && string.IsNullOrWhiteSpace(overrideReason))
            {
                throw new BusinessRuleException(
                    "Bắt buộc phải nhập lý do khi đưa ra quyết định khác với gợi ý sơ bộ của hệ thống.");
            }

            return new ScreeningDecision
            {
                ApplicationId = applicationId,
                InterviewId = interviewId,
                DecidedByUserId = decidedByUserId,
                DecidedByRole = decidedByRole,
                DecidedAt = DateTime.UtcNow,
                DecisionType = decisionType,
                SystemRecommendation = systemRecommendation,
                SystemOverallScore = systemOverallScore,
                EvaluationSnapshot = evaluationSnapshot ?? new List<QuestionEvaluationSnapshotItem>(),
                ScoredQuestionCount = scoredQuestionCount,
                TotalQuestionCount = totalQuestionCount,
                OverrideReason = overrideReason?.Trim(),
                Notes = notes?.Trim()
            };
        }
    }
}

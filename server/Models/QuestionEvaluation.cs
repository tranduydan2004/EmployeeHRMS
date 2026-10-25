using System;
using System.Collections.Generic;

namespace EmployeeHRMS.Api.Models
{
    /// <summary>
    /// Entity lưu vết lịch sử đánh giá từng câu trả lời theo rubric (Audit Trail).
    /// Mỗi lần chấm lại (Re-evaluate) sinh ra một bản ghi mới với VersionNumber tăng dần.
    /// Bảng được áp dụng PostgreSQL Partial Unique Index:
    /// CREATE UNIQUE INDEX "IX_QuestionEvaluations_InterviewAnswerId_Active"
    /// ON "QuestionEvaluations" ("InterviewAnswerId") WHERE "IsActive" = true;
    /// </summary>
    public class QuestionEvaluation
    {
        public int Id { get; set; }

        public int InterviewAnswerId { get; set; }
        public InterviewAnswer InterviewAnswer { get; set; } = null!;

        public int VersionNumber { get; set; }

        /// <summary>
        /// Snapshot bản text gửi vào model chấm (Transcript gốc hoặc EditedTranscript lúc bấm chấm lại).
        /// </summary>
        public string TranscriptSnapshot { get; set; } = string.Empty;

        public RubricLevel? Level { get; private set; }
        public decimal? Score { get; private set; }
        public string? Analysis { get; private set; }

        public List<string> EvidenceQuotes { get; private set; } = new();
        public List<string> Strengths { get; private set; } = new();
        public List<string> Concerns { get; private set; } = new();

        public string ModelName { get; set; } = string.Empty;
        public string PromptVersion { get; set; } = string.Empty;

        public EvaluationOutcome Outcome { get; private set; }
        public string? Reason { get; private set; }
        public string? ErrorMessage { get; private set; }

        public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
        public int? EvaluatedByUserId { get; set; }
        public int? ConfirmedByUserId { get; private set; }
        public DateTime? ConfirmedAt { get; private set; }

        public bool IsActive { get; private set; } = true;

        // Constructor mặc định cho EF Core
        public QuestionEvaluation() { }

        /// <summary>
        /// Tạo bản đánh giá thành công (Succeeded).
        /// </summary>
        public static QuestionEvaluation CreateSucceeded(
            int interviewAnswerId,
            int versionNumber,
            string transcriptSnapshot,
            RubricLevel level,
            decimal score,
            string analysis,
            List<string>? evidenceQuotes,
            List<string>? strengths,
            List<string>? concerns,
            string modelName,
            string promptVersion,
            int? evaluatedByUserId = null)
        {
            return new QuestionEvaluation
            {
                InterviewAnswerId = interviewAnswerId,
                VersionNumber = versionNumber,
                TranscriptSnapshot = transcriptSnapshot,
                Level = level,
                Score = score,
                Analysis = analysis,
                EvidenceQuotes = evidenceQuotes ?? new List<string>(),
                Strengths = strengths ?? new List<string>(),
                Concerns = concerns ?? new List<string>(),
                ModelName = modelName,
                PromptVersion = promptVersion,
                Outcome = EvaluationOutcome.Succeeded,
                EvaluatedByUserId = evaluatedByUserId,
                EvaluatedAt = DateTime.UtcNow,
                IsActive = true
            };
        }

        /// <summary>
        /// Tạo bản đánh giá khi câu trả lời không thể chấm điểm (NotScorable: OffTopic, MissingRubric, v.v.).
        /// </summary>
        public static QuestionEvaluation CreateNotScorable(
            int interviewAnswerId,
            int versionNumber,
            string transcriptSnapshot,
            string reason,
            string? analysis,
            string modelName,
            string promptVersion,
            int? evaluatedByUserId = null)
        {
            return new QuestionEvaluation
            {
                InterviewAnswerId = interviewAnswerId,
                VersionNumber = versionNumber,
                TranscriptSnapshot = transcriptSnapshot,
                Level = null,
                Score = null,
                Analysis = analysis,
                ModelName = modelName,
                PromptVersion = promptVersion,
                Outcome = EvaluationOutcome.NotScorable,
                Reason = reason,
                EvaluatedByUserId = evaluatedByUserId,
                EvaluatedAt = DateTime.UtcNow,
                IsActive = true
            };
        }

        /// <summary>
        /// Tạo bản đánh giá khi câu trả lời là khoảng lặng hoặc ảo giác (NoResponse: Silence, KnownHallucination).
        /// </summary>
        public static QuestionEvaluation CreateNoResponse(
            int interviewAnswerId,
            int versionNumber,
            string transcriptSnapshot,
            string reason,
            string modelName,
            string promptVersion,
            int? evaluatedByUserId = null)
        {
            return new QuestionEvaluation
            {
                InterviewAnswerId = interviewAnswerId,
                VersionNumber = versionNumber,
                TranscriptSnapshot = transcriptSnapshot,
                Level = null,
                Score = null,
                Analysis = null,
                ModelName = modelName,
                PromptVersion = promptVersion,
                Outcome = EvaluationOutcome.NoResponse,
                Reason = reason,
                EvaluatedByUserId = evaluatedByUserId,
                EvaluatedAt = DateTime.UtcNow,
                IsActive = true
            };
        }

        /// <summary>
        /// Tạo bản ghi thất bại kỹ thuật trong quá trình chấm điểm (Failed).
        /// Lưu ý: Theo D9, nếu trước đó đã có bản đánh giá Succeeded/NotScorable đang IsActive = true,
        /// thì bản Failed này được tạo với IsActive = false để lưu audit trail mà không đè bản active cũ.
        /// </summary>
        public static QuestionEvaluation CreateFailed(
            int interviewAnswerId,
            int versionNumber,
            string transcriptSnapshot,
            string errorMessage,
            string modelName,
            string promptVersion,
            bool isActive = false,
            int? evaluatedByUserId = null)
        {
            return new QuestionEvaluation
            {
                InterviewAnswerId = interviewAnswerId,
                VersionNumber = versionNumber,
                TranscriptSnapshot = transcriptSnapshot,
                Level = null,
                Score = null,
                ErrorMessage = errorMessage,
                ModelName = modelName,
                PromptVersion = promptVersion,
                Outcome = EvaluationOutcome.Failed,
                EvaluatedByUserId = evaluatedByUserId,
                EvaluatedAt = DateTime.UtcNow,
                IsActive = isActive
            };
        }

        /// <summary>
        /// Hủy kích hoạt bản đánh giá hiện hành (khi bản mới chuẩn bị active).
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
        }

        /// <summary>
        /// HR xác nhận bản đánh giá này.
        /// </summary>
        public void Confirm(int userId)
        {
            ConfirmedByUserId = userId;
            ConfirmedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Xóa/làm sạch dữ liệu văn bản cá nhân (TranscriptSnapshot, EvidenceQuotes, Analysis, Strengths, Concerns)
        /// theo yêu cầu quyền riêng tư (Luật số 91/2025/QH15).
        /// Giữ nguyên Level, Score, Outcome để bảo toàn kiểm toán kết quả.
        /// </summary>
        public void AnonymizePersonalData()
        {
            TranscriptSnapshot = "[Đã xóa theo chính sách bảo mật dữ liệu cá nhân]";
            Analysis = null;
            EvidenceQuotes = new List<string>();
            Strengths = new List<string>();
            Concerns = new List<string>();
        }
    }
}

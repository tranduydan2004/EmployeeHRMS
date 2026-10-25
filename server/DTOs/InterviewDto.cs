using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EmployeeHRMS.Api.Models.ValueObjects;

namespace EmployeeHRMS.Api.DTOs
{
    // === DTO cho tạo mới Interview ===
    public class InterviewCreateDto
    {
        [Required(ErrorMessage = "Application ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Application ID must be a positive integer.")]
        public int ApplicationId { get; set; }

        [Required(ErrorMessage = "Scheduled date is required.")]
        public DateTime ScheduledDate { get; set; }

        // Interviewer được gán (nullable — Admin/HR gán khi tạo lịch phỏng vấn)
        public int? InterviewerId { get; set; }
    }

    // === DTO cho thêm câu hỏi phỏng vấn ===
    public class InterviewQuestionCreateDto
    {
        [Required(ErrorMessage = "Question is required.")]
        [StringLength(2000, MinimumLength = 5, ErrorMessage = "Question must be between 5 and 2000 characters.")]
        public string Question { get; set; } = string.Empty;

        [Required(ErrorMessage = "Order index is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Order index must be a non-negative integer.")]
        public int OrderIndex { get; set; }
    }

    // === DTO trả về Interview — kèm danh sách câu hỏi (nested projection) ===
    public class InterviewResponseDto
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public string CandidateName { get; set; } = string.Empty;
        public string CandidateEmail { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string ApplicationStatus { get; set; } = string.Empty;
        public int? InterviewerId { get; set; }
        public string? InterviewerName { get; set; }
        public DateTime ScheduledDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal? AiOverallScore { get; set; }
        public string? AiOverallSummary { get; set; }
        public string? Recommendation { get; set; }
        public bool IsPersonalDataErased { get; set; }
        public Guid? InviteToken { get; set; }
        public DateTime? InviteExpiresAt { get; set; }
        public string? InviteUrl { get; set; }

        // Nested projection: List<InterviewQuestion> → List<InterviewQuestionResponseDto>
        public List<InterviewQuestionResponseDto> Questions { get; set; } = new();
    }

    // === DTO cập nhật câu trả lời phỏng vấn ===
    public class InterviewQuestionAnswerDto
    {
        [Required(ErrorMessage = "Candidate answer is required.")]
        [StringLength(2000, ErrorMessage = "Candidate answer must not exceed 2000 characters.")]
        public string CandidateAnswer { get; set; } = string.Empty;
    }

    // === DTO trả về cho từng câu hỏi phỏng vấn ===
    public class InterviewQuestionResponseDto
    {
        public int Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public string? CandidateAnswer { get; set; }
        public decimal? AiQuestionScore { get; set; }
        public string? AiFeedback { get; set; }
        public int OrderIndex { get; set; }
        public string? Category { get; set; }
        public ScoringRubric? ScoringRubric { get; set; }
        public InterviewAnswerDetailDto? Answer { get; set; }
    }

    // === DTO hoàn tất buổi phỏng vấn ===
    public class InterviewCompleteDto
    {
        [StringLength(4000, ErrorMessage = "Summary must not exceed 4000 characters.")]
        public string? Summary { get; set; }
    }

    // === DTO phân công Interviewer cho buổi phỏng vấn (Phase 5) ===
    public class AssignInterviewerDto
    {
        [Required(ErrorMessage = "Interviewer ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Interviewer ID must be a positive integer.")]
        public int InterviewerId { get; set; }
    }

    // === DTO chỉnh sửa transcript cho HR / Interviewer (Phase 4 & 5) ===
    public class UpdateTranscriptDto
    {
        [Required(ErrorMessage = "Transcript is required.")]
        [StringLength(10000, MinimumLength = 1, ErrorMessage = "Transcript must be between 1 and 10000 characters.")]
        public string Transcript { get; set; } = string.Empty;

        /// <summary>
        /// Phiên bản client đang nhìn thấy (Optimistic Concurrency).
        /// Nếu server có EditVersion > ExpectedVersion -> trả 409 Conflict.
        /// </summary>
        public int? ExpectedVersion { get; set; }
    }

    // === DTO tạo quyết định sơ loại bất biến (Phase 5) ===
    public class ScreeningDecisionCreateDto
    {
        [Required(ErrorMessage = "Decision type is required.")]
        public Models.ScreeningDecisionType DecisionType { get; set; }

        [StringLength(1000, ErrorMessage = "Override reason must not exceed 1000 characters.")]
        public string? OverrideReason { get; set; }

        [StringLength(2000, ErrorMessage = "Notes must not exceed 2000 characters.")]
        public string? Notes { get; set; }
    }

    // === DTO trả về quyết định sơ loại (Phase 5) ===
    public class ScreeningDecisionResponseDto
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public int InterviewId { get; set; }
        public int DecidedByUserId { get; set; }
        public string? DecidedByUserName { get; set; }
        public string DecidedByRole { get; set; } = string.Empty;
        public DateTime DecidedAt { get; set; }
        public string DecisionType { get; set; } = string.Empty;
        public string SystemRecommendation { get; set; } = string.Empty;
        public decimal? SystemOverallScore { get; set; }
        public List<QuestionEvaluationSnapshotItem> EvaluationSnapshot { get; set; } = new();
        public int ScoredQuestionCount { get; set; }
        public int TotalQuestionCount { get; set; }
        public string? OverrideReason { get; set; }
        public string? Notes { get; set; }
    }

    // === DTO kết quả kích hoạt Re-evaluate (Phase 4) ===
    public class ReevaluateResponseDto
    {
        public int InterviewAnswerId { get; set; }
        public int QuestionId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime EnqueuedAt { get; set; } = DateTime.UtcNow;
    }

    // === DTO trả về lịch sử đánh giá rubric (Audit Trail) (Phase 4) ===
    public class QuestionEvaluationResponseDto
    {
        public int Id { get; set; }
        public int InterviewAnswerId { get; set; }
        public int VersionNumber { get; set; }
        public string TranscriptSnapshot { get; set; } = string.Empty;
        public string? Level { get; set; }
        public decimal? Score { get; set; }
        public string? Analysis { get; set; }
        public List<string> EvidenceQuotes { get; set; } = new();
        public List<string> Strengths { get; set; } = new();
        public List<string> Concerns { get; set; } = new();
        public string ModelName { get; set; } = string.Empty;
        public string PromptVersion { get; set; } = string.Empty;
        public string Outcome { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime EvaluatedAt { get; set; }
        public int? EvaluatedByUserId { get; set; }
        public bool IsActive { get; set; }
    }

    // === DTO chi tiết InterviewAnswer cho HR / Interviewer (Phase 4 & 5) ===
    public class InterviewAnswerDetailDto
    {
        public int Id { get; set; }
        public int InterviewQuestionId { get; set; }
        public string AudioStorageKey { get; set; } = string.Empty;
        public int AudioDurationSeconds { get; set; }
        public double? SttDurationSeconds { get; set; }
        public string AudioMimeType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? FailedStage { get; set; }
        public string? Transcript { get; set; }
        public string? EditedTranscript { get; set; }
        public string? EffectiveTranscript { get; set; }
        public int EditVersion { get; set; }
        public int? LastEditedByUserId { get; set; }
        public DateTime? LastEditedAt { get; set; }
        public string? DetectedLanguage { get; set; }
        public SpeechMetrics? SpeechMetrics { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsAudioDeleted { get; set; }
        public List<QuestionEvaluationResponseDto> Evaluations { get; set; } = new();
    }

    // === DTO yêu cầu xóa dữ liệu cá nhân (Phase 5 - Step 6: Right to Erasure) ===
    public class ErasePersonalDataDto
    {
        [Required(ErrorMessage = "Lý do xóa dữ liệu cá nhân là bắt buộc.")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Lý do xóa phải từ 5 đến 500 ký tự.")]
        public string Reason { get; set; } = string.Empty;
    }

    // === DTO kết quả xóa dữ liệu cá nhân (Phase 5 - Step 6) ===
    public class ErasePersonalDataResponseDto
    {
        public int InterviewId { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ErasedAnswersCount { get; set; }
        public int ErasedAudioFilesCount { get; set; }
        public int ErasedEvaluationsCount { get; set; }
        public DateTime ErasedAt { get; set; } = DateTime.UtcNow;
        public string ErasedByUserName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}

using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.DTOs.Common;
using EmployeeHRMS.Api.Models;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Interface cho Interview Service — CRUD async + quản lý câu hỏi
    /// </summary>
    public interface IInterviewService
    {
        Task<PagedResult<InterviewResponseDto>> GetAllAsync(PaginationParams paginationParams);
        Task<InterviewResponseDto?> GetByIdAsync(int id);
        Task<IEnumerable<InterviewResponseDto>> GetByApplicationAsync(int applicationId);
        Task<InterviewResponseDto> CreateAsync(InterviewCreateDto dto);
        Task<bool> AddQuestionAsync(int interviewId, InterviewQuestionCreateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<InterviewResponseDto?> CompleteInterviewAsync(int id, string? summary = null);

        /// <summary>Fetch entity cho authorization handler (không DTO).</summary>
        Task<Interview?> GetEntityByIdAsync(int id);

        /// <summary>Fetch entity kèm Questions và Candidate phục vụ đánh giá/trả lời câu hỏi.</summary>
        Task<Interview?> GetEntityWithQuestionsAsync(int id);

        /// <summary>Cập nhật câu trả lời của ứng viên cho câu hỏi phỏng vấn.</summary>
        Task<bool> UpdateQuestionAnswerAsync(InterviewQuestion question, string candidateAnswer);

        // ============================================================
        // Candidate Audio Interview (Phase 2)
        // ============================================================

        /// <summary>
        /// Tạo phiên phỏng vấn audio và link mời cho Application (HR).
        /// Copy câu hỏi từ QuestionBankItem của JobPosting (chỉ khi JD đã Approved/Published).
        /// Chuyển trạng thái Application sang Interviewing.
        /// </summary>
        Task<InterviewInviteResponseDto> CreateInviteAsync(int applicationId, InterviewInviteCreateDto? dto = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy thông tin phiên phỏng vấn và danh sách câu hỏi cho ứng viên (công khai qua inviteToken).
        /// TUYỆT ĐỐI không trả ScoringRubric hay dữ liệu nội bộ.
        /// </summary>
        Task<CandidateInterviewSessionResponseDto> GetCandidateSessionAsync(Guid inviteToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Ứng viên xác nhận bắt đầu phỏng vấn và đồng ý ghi âm (idempotent).
        /// </summary>
        Task<CandidateInterviewStartResponseDto> StartCandidateInterviewAsync(Guid inviteToken, CancellationToken cancellationToken = default);

        // ============================================================
        // AI Rubric Grading & Re-evaluation Pipeline (Phase 4)
        // ============================================================

        /// <summary>
        /// HR/Interviewer chỉnh sửa bản bóc băng của câu trả lời.
        /// </summary>
        Task<InterviewAnswerDetailDto> UpdateTranscriptAsync(
            int interviewId,
            int questionId,
            string editedTranscript,
            int currentUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Kích hoạt chấm lại (Re-evaluate) cho câu trả lời sau khi sửa transcript hoặc cấu hình rubric.
        /// Chuyển trạng thái sang Scoring và nạp job vào Channel queue.
        /// </summary>
        Task<ReevaluateResponseDto> ReevaluateAnswerAsync(
            int interviewId,
            int questionId,
            int currentUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy toàn bộ lịch sử đánh giá (Audit Trail) của tất cả câu hỏi trong buổi phỏng vấn.
        /// </summary>
        Task<List<QuestionEvaluationResponseDto>> GetInterviewEvaluationsAsync(
            int interviewId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy toàn bộ lịch sử đánh giá (Audit Trail) của một câu hỏi cụ thể.
        /// </summary>
        Task<List<QuestionEvaluationResponseDto>> GetQuestionEvaluationsAsync(
            int interviewId,
            int questionId,
            CancellationToken cancellationToken = default);
    }
}

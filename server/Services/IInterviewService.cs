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
        /// Chuyển trạng thái Application sang Screening.
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

        /// <summary>
        /// Gửi lại email lời mời phỏng vấn cho ứng viên (Admin/HR).
        /// </summary>
        Task<ResendInviteResponseDto> ResendInviteAsync(int interviewId, CancellationToken cancellationToken = default);

        // ============================================================
        // AI Rubric Grading & Re-evaluation Pipeline (Phase 4 & 5)
        // ============================================================

        /// <summary>
        /// HR/Interviewer chỉnh sửa bản bóc băng của câu trả lời. Hỗ trợ Optimistic Concurrency qua expectedVersion.
        /// </summary>
        Task<InterviewAnswerDetailDto> UpdateTranscriptAsync(
            int interviewId,
            int questionId,
            string editedTranscript,
            int currentUserId,
            int? expectedVersion = null,
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

        // ============================================================
        // HR/Interviewer Review & Governance (Phase 5)
        // ============================================================

        /// <summary>
        /// Phân công Interviewer phụ trách đánh giá buổi phỏng vấn (Admin/HR).
        /// </summary>
        Task<InterviewResponseDto> AssignInterviewerAsync(
            int interviewId,
            int interviewerId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tạo quyết định sơ loại bất biến (ScreeningDecision) kèm snapshot bằng chứng và lý do ghi đè (Admin/HR).
        /// </summary>
        Task<ScreeningDecisionResponseDto> CreateScreeningDecisionAsync(
            int interviewId,
            ScreeningDecisionCreateDto dto,
            int currentUserId,
            string currentRole,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy danh sách lịch sử các quyết định sơ loại (Audit Trail) của buổi phỏng vấn.
        /// </summary>
        Task<IEnumerable<ScreeningDecisionResponseDto>> GetScreeningDecisionsAsync(
            int interviewId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy audio stream cho playback và ghi nhận AudioAccessLog bảo vệ quyền riêng tư theo Luật Bảo vệ dữ liệu cá nhân (Luật số 91/2025/QH15).
        /// </summary>
        Task<(Stream Stream, string ContentType)> GetAudioStreamWithAccessLogAsync(
            string storageKey,
            int userId,
            string userRole,
            string? ipAddress,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy danh sách tóm tắt phỏng vấn sơ loại cho Dashboard (chiếu qua .Select() tối ưu, không nạp transcript/jsonb).
        /// </summary>
        Task<PagedResult<InterviewDashboardSummaryDto>> GetDashboardSummaryAsync(
            InterviewDashboardFilterParams filters,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Thống kê nhanh các chỉ số KPI cho Dashboard phỏng vấn sơ loại (Executive Summary).
        /// </summary>
        Task<InterviewDashboardStatsDto> GetDashboardStatsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy danh sách các Interviewer khả dụng để HR/Admin phân công phụ trách.
        /// </summary>
        Task<List<InterviewerOptionDto>> GetAvailableInterviewersAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Xóa dữ liệu cá nhân (file ghi âm, transcript, trích dẫn bằng chứng, phân tích AI) của buổi phỏng vấn (Admin/HR).
        /// Bảo lưu điểm số rubric và quyết định sơ loại theo luật số 91/2025/QH15.
        /// </summary>
        Task<ErasePersonalDataResponseDto> ErasePersonalDataAsync(
            int interviewId,
            ErasePersonalDataDto dto,
            int currentUserId,
            string currentRole,
            CancellationToken cancellationToken = default);
    }
}

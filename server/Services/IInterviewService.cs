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
    }
}

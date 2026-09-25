using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.DTOs.Common;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Interface cho JobPosting Service — CRUD async + filter methods + Phase 1 JD Generation
    /// </summary>
    public interface IJobPostingService
    {
        // --- Existing CRUD ---
        Task<PagedResult<JobPostingResponseDto>> GetAllAsync(JobPostingQueryParams queryParams);
        Task<JobPostingResponseDto?> GetByIdAsync(int id);
        Task<IEnumerable<JobPostingResponseDto>> GetActiveJobsAsync();
        Task<IEnumerable<JobPostingResponseDto>> GetByDepartmentAsync(int departmentId);
        Task<JobPostingResponseDto> CreateAsync(JobPostingCreateDto dto);
        Task<bool> UpdateAsync(int id, JobPostingUpdateDto dto);
        Task<bool> DeleteAsync(int id);

        // --- Phase 1: Smart JD & Question Bank Generation ---
        Task<JobPostingDetailResponseDto> DraftJdAsync(DraftJdRequestDto dto, string? createdBy = null);
        Task<JobPostingDetailResponseDto> UpdateJdContentAsync(int id, UpdateJdContentDto dto);
        Task<JobPostingDetailResponseDto> ApproveJdAsync(int id);
        Task<JobPostingDetailResponseDto> GetDetailByIdAsync(int id);
        Task<List<QuestionBankItemResponseDto>> GetQuestionsAsync(int jobPostingId);
    }
}

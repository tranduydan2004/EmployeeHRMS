using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.DTOs.Common;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Interface cho JobPosting Service — CRUD async + filter methods
    /// </summary>
    public interface IJobPostingService
    {
        Task<PagedResult<JobPostingResponseDto>> GetAllAsync(JobPostingQueryParams queryParams);
        Task<JobPostingResponseDto?> GetByIdAsync(int id);
        Task<IEnumerable<JobPostingResponseDto>> GetActiveJobsAsync();
        Task<IEnumerable<JobPostingResponseDto>> GetByDepartmentAsync(int departmentId);
        Task<JobPostingResponseDto> CreateAsync(JobPostingCreateDto dto);
        Task<bool> UpdateAsync(int id, JobPostingUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }
}

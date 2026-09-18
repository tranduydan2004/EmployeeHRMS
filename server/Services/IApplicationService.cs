using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.DTOs.Common;
using EmployeeHRMS.Api.Models;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Interface cho Application Service — CRUD async + nghiệp vụ chuyển trạng thái
    /// </summary>
    public interface IApplicationService
    {
        Task<PagedResult<ApplicationResponseDto>> GetAllAsync(PaginationParams paginationParams);
        Task<ApplicationResponseDto?> GetByIdAsync(int id);
        Task<IEnumerable<ApplicationResponseDto>> GetByStatusAsync(ApplicationStatus status);
        Task<IEnumerable<ApplicationResponseDto>> GetByCandidateAsync(int candidateId);
        Task<IEnumerable<ApplicationResponseDto>> GetByJobPostingAsync(int jobPostingId);
        Task<ApplicationResponseDto> CreateAsync(ApplicationCreateDto dto);

        // Nghiệp vụ chuyển trạng thái: Applied → Screening → Interviewing → Offered/Rejected
        Task<bool> UpdateStatusAsync(int id, ApplicationStatus newStatus);
        Task<bool> DeleteAsync(int id);

        /// <summary>Fetch entity cho authorization handler (không DTO).</summary>
        Task<Application?> GetEntityByIdAsync(int id);
    }
}

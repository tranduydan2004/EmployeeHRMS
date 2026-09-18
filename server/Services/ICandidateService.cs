using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.DTOs.Common;
using EmployeeHRMS.Api.Models;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Interface cho Candidate Service — CRUD async + search
    /// </summary>
    public interface ICandidateService
    {
        Task<PagedResult<CandidateResponseDto>> GetAllAsync(PaginationParams paginationParams);
        Task<CandidateResponseDto?> GetByIdAsync(int id);
        Task<IEnumerable<CandidateResponseDto>> SearchAsync(string keyword);
        Task<CandidateResponseDto> CreateAsync(CandidateCreateDto dto);
        Task<bool> UpdateAsync(int id, CandidateUpdateDto dto);
        Task<bool> DeleteAsync(int id);

        /// <summary>Fetch entity cho authorization handler (không DTO).</summary>
        Task<Candidate?> GetEntityByIdAsync(int id);
    }
}

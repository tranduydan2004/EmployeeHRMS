using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.DTOs.Common;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Interface cho Department Service — CRUD async
    /// </summary>
    public interface IDepartmentService
    {
        Task<PagedResult<DepartmentResponseDto>> GetAllAsync(PaginationParams paginationParams);
        Task<PagedResult<DepartmentPublicDto>> GetAllPublicAsync(PaginationParams paginationParams);
        Task<DepartmentResponseDto?> GetByIdAsync(int id);
        Task<DepartmentResponseDto?> GetWithEmployeesAsync(int id);
        Task<DepartmentResponseDto> CreateAsync(DepartmentCreateDto dto);
        Task<bool> UpdateAsync(int id, DepartmentUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }
}

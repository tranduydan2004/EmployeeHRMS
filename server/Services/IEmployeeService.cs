using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.DTOs.Common;
using EmployeeHRMS.Api.Models;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Interface cho Employee Service — tất cả method đều async (Task)
    /// Minh họa: Interface, async pattern, Func delegate parameter
    /// </summary>
    public interface IEmployeeService
    {
        // === Read operations ===
        Task<PagedResult<EmployeeResponseDto>> GetAllAsync(PaginationParams paginationParams);
        Task<EmployeeResponseDto?> GetByIdAsync(int employeeId);
        Task<IEnumerable<EmployeeResponseDto>> FindByNameAsync(string name);
        Task<IEnumerable<EmployeeResponseDto>> FindByDepartmentAsync(int departmentId);
        Task<IEnumerable<EmployeeResponseDto>> GetTopSalaryAsync(int count = 10);

        // Nhận Func<Employee, bool> delegate — cho phép caller truyền bất kỳ filter logic nào
        Task<IEnumerable<EmployeeResponseDto>> SearchAsync(Func<Employee, bool> predicate);

        // LINQ GroupBy → Statistics projection
        Task<IEnumerable<EmployeeStatisticsDto>> GetStatisticsByDepartmentAsync();

        // Lọc nhân viên theo trạng thái (Active, OnLeave, Resigned)
        Task<IEnumerable<EmployeeResponseDto>> GetByStatusAsync(EmployeeStatus status);

        // Fetch entity phục vụ resource-based authorization
        Task<Employee?> GetEntityByIdAsync(int employeeId);

        // Lấy thông tin hồ sơ nhân viên của user hiện tại
        Task<EmployeeResponseDto> GetMeAsync(int userId);

        // Lấy danh sách đồng nghiệp cùng phòng ban (chỉ thông tin cơ bản, không có lương/status)
        Task<IEnumerable<EmployeeColleagueDto>> GetMyDepartmentColleaguesAsync(int userId);

        // Lấy linked ApplicationUser.Id của employee (nếu có)
        Task<int?> GetLinkedUserIdAsync(int employeeId);

        // === Write operations ===
        Task<EmployeeCreateResponseDto> CreateAsync(EmployeeCreateDto dto);
        Task<bool> UpdateAsync(int employeeId, EmployeeUpdateDto dto);
        Task<bool> ChangeStatusAsync(int employeeId, EmployeeStatus status);
        Task<bool> DeleteAsync(int employeeId);
        Task<int> DeleteAllAsync();
    }
}

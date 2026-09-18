using System.Collections.Generic;
using System.Security.Claims;
using EmployeeHRMS.Api.Authorization;
using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.DTOs.Common;
using EmployeeHRMS.Api.Exceptions;
using EmployeeHRMS.Api.Models;
using EmployeeHRMS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeHRMS.Api.Controllers
{
    /// <summary>
    /// Employees Controller — RESTful API
    /// Resource-Based Authorization:
    /// - Admin, HR: Toàn quyền truy cập mọi endpoint
    /// - Employee: Chỉ được xem hồ sơ chính mình (GET me hoặc GET {id}) và danh sách phòng ban (GET my-department)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")] // Route: api/employees
    [Authorize]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly IAuthorizationService _authorizationService;
        private readonly INotificationService _notificationService;

        // Constructor injection — ASP.NET Core DI inject services
        public EmployeesController(
            IEmployeeService employeeService,
            IAuthorizationService authorizationService,
            INotificationService notificationService)
        {
            _employeeService = employeeService;
            _authorizationService = authorizationService;
            _notificationService = notificationService;
        }

        // POST: api/employees — Chỉ Admin, HR
        [HttpPost]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create([FromBody] EmployeeCreateDto dto)
        {
            var result = await _employeeService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { employeeId = result.EmployeeId }, result);
        }

        // GET: api/employees?pageNumber=1&pageSize=10&sortBy=salary&isDescending=true — Chỉ Admin, HR
        [HttpGet]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> GetAll([FromQuery] PaginationParams paginationParams)
        {
            var result = await _employeeService.GetAllAsync(paginationParams);
            return Ok(result);
        }

        // GET: api/employees/me — Cho Employee (hoặc user đã link Employee) xem hồ sơ của chính mình
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var employeeDto = await _employeeService.GetMeAsync(userId);
            return Ok(employeeDto);
        }

        // GET: api/employees/my-department — Cho Employee xem đồng nghiệp cùng phòng ban (không lộ lương/status)
        [HttpGet("my-department")]
        public async Task<IActionResult> GetMyDepartment()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var colleagues = await _employeeService.GetMyDepartmentColleaguesAsync(userId);
            return Ok(colleagues);
        }

        // GET: api/employees/status/{status} — Lọc nhân viên theo trạng thái (Chỉ Admin, HR)
        [HttpGet("status/{status}")]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> GetByStatus(EmployeeStatus status)
        {
            var employees = await _employeeService.GetByStatusAsync(status);
            return Ok(employees);
        }

        // GET: api/employees/{employeeId} — Resource-Based Authorization (Admin/HR xem tất cả, Employee chỉ xem chính mình)
        [HttpGet("{employeeId:int}")]
        public async Task<IActionResult> GetById(int employeeId)
        {
            var employee = await _employeeService.GetEntityByIdAsync(employeeId)
                ?? throw new NotFoundException("Employee", employeeId);

            var authResult = await _authorizationService.AuthorizeAsync(User, employee, ResourceOperations.Read);
            if (!authResult.Succeeded) return Forbid();

            var dto = await _employeeService.GetByIdAsync(employeeId);
            return Ok(dto);
        }

        // GET: api/employees/search?name={name} [DEPRECATED] — Chỉ Admin, HR
        [HttpGet("search")]
        [Authorize(Roles = "Admin,HR")]
        [Obsolete("Use GET /api/employees with paginationParams.Search instead.")]
        public async Task<IActionResult> SearchByName([FromQuery] string name)
        {
            var employees = await _employeeService.FindByNameAsync(name);
            return Ok(employees);
        }

        // GET: api/employees/by-department/{departmentId} — Chỉ Admin, HR
        [HttpGet("by-department/{departmentId}")]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> GetByDepartment(int departmentId)
        {
            var employees = await _employeeService.FindByDepartmentAsync(departmentId);
            return Ok(employees);
        }

        // GET: api/employees/top-salary?count=10 — Chỉ Admin, HR
        [HttpGet("top-salary")]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> GetTopSalary([FromQuery] int count = 10)
        {
            var employees = await _employeeService.GetTopSalaryAsync(count);
            return Ok(employees);
        }

        // GET: api/employees/statistics — Chỉ Admin, HR
        [HttpGet("statistics")]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> GetStatistics()
        {
            var statistics = await _employeeService.GetStatisticsByDepartmentAsync();
            return Ok(statistics);
        }

        // PUT: api/employees/{employeeId} — Chỉ Admin, HR
        [HttpPut("{employeeId:int}")]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Update(int employeeId, [FromBody] EmployeeUpdateDto dto)
        {
            await _employeeService.UpdateAsync(employeeId, dto);
            return NoContent();
        }

        // PATCH: api/employees/{employeeId}/status — Chỉ Admin, HR
        [HttpPatch("{employeeId:int}/status")]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> ChangeStatus(int employeeId, [FromBody] EmployeeStatus status)
        {
            await _employeeService.ChangeStatusAsync(employeeId, status);

            var linkedUserId = await _employeeService.GetLinkedUserIdAsync(employeeId);
            if (linkedUserId.HasValue)
            {
                await _notificationService.SendToUserAsync(
                    linkedUserId.Value,
                    "Cập nhật trạng thái nhân sự",
                    "EMPLOYEE_STATUS_CHANGED",
                    new Dictionary<string, string>
                    {
                        { "status", status.ToString() }
                    },
                    NotificationType.Info,
                    relatedEntity: "Employee",
                    relatedEntityId: employeeId,
                    fallbackMessage: $"Trạng thái nhân sự của bạn đã được cập nhật thành: {status}.");
            }

            return NoContent();
        }

        // DELETE: api/employees/{employeeId} — Chỉ Admin, HR
        [HttpDelete("{employeeId:int}")]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Delete(int employeeId)
        {
            await _employeeService.DeleteAsync(employeeId);
            return NoContent();
        }

        // DELETE: api/employees — Chỉ Admin, HR
        [HttpDelete]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> DeleteAll()
        {
            var count = await _employeeService.DeleteAllAsync();
            return Ok(new { DeletedCount = count, Message = $"Deleted {count} employee(s)." });
        }
    }
}

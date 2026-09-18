using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.DTOs.Common;
using EmployeeHRMS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeHRMS.Api.Controllers
{
    /// <summary>
    /// Departments Controller — CRUD + query endpoints
    /// Quyền truy cập: Admin, HR
    /// </summary>
    [ApiController]
    [Route("api/[controller]")] // Route: api/departments
    [Authorize(Roles = "Admin,HR")]
    public class DepartmentsController : ControllerBase
    {
        private readonly IDepartmentService _departmentService;

        public DepartmentsController(IDepartmentService departmentService)
        {
            _departmentService = departmentService;
        }

        // GET: api/departments
        // Cho phép truy cập công khai (Anonymous, Candidate, Employee, Interviewer) nhưng thu gọn DTO
        // Admin, HR nhận PagedResult<DepartmentResponseDto> đầy đủ; Các role khác nhận PagedResult<DepartmentPublicDto>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] PaginationParams paginationParams)
        {
            bool isPrivileged = User.IsInRole("Admin") || User.IsInRole("HR");
            if (!isPrivileged)
            {
                var publicDepartments = await _departmentService.GetAllPublicAsync(paginationParams);
                return Ok(publicDepartments);
            }

            var departments = await _departmentService.GetAllAsync(paginationParams);
            return Ok(departments);
        }

        // GET: api/departments/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var department = await _departmentService.GetByIdAsync(id);
            return Ok(department);
        }

        // GET: api/departments/{id}/employees
        // Trả về department kèm danh sách nhân viên (projection)
        [HttpGet("{id}/employees")]
        public async Task<IActionResult> GetWithEmployees(int id)
        {
            var department = await _departmentService.GetWithEmployeesAsync(id);
            return Ok(department);
        }

        // POST: api/departments
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DepartmentCreateDto dto)
        {
            var department = await _departmentService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = department.Id }, department);
        }

        // PUT: api/departments/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] DepartmentUpdateDto dto)
        {
            await _departmentService.UpdateAsync(id, dto);
            return NoContent();
        }

        // DELETE: api/departments/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _departmentService.DeleteAsync(id);
            return NoContent();
        }
    }
}

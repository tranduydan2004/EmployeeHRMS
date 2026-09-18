using System.ComponentModel.DataAnnotations;

namespace EmployeeHRMS.Api.DTOs
{
    // === DTO cho tạo mới Department ===
    public class DepartmentCreateDto
    {
        [Required(ErrorMessage = "Department name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Department name must be between 2 and 100 characters.")]
        public string Name { get; set; } = string.Empty;
    }

    // === DTO cho cập nhật Department ===
    public class DepartmentUpdateDto
    {
        [Required(ErrorMessage = "Department name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Department name must be between 2 and 100 characters.")]
        public string Name { get; set; } = string.Empty;
    }

    // === DTO công khai rút gọn (cho khách vãng lai, ứng viên, nhân viên) ===
    // Bảo vệ thông tin nội bộ: KHÔNG chứa EmployeeCount và EmployeeNames
    public class DepartmentPublicDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    // === DTO trả về cho client có quyền (Admin, HR) — Projection từ Department entity ===
    // Chứa thêm thông tin computed: EmployeeCount, EmployeeNames (từ navigation property)
    public class DepartmentResponseDto : DepartmentPublicDto
    {
        public int EmployeeCount { get; set; }
        public List<string> EmployeeNames { get; set; } = new();
    }
}

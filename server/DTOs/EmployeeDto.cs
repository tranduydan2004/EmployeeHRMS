using System.ComponentModel.DataAnnotations;

namespace EmployeeHRMS.Api.DTOs
{
    // === DTO cho tạo mới Employee — khớp với model fields ===
    public class EmployeeCreateDto
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 200 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(200, ErrorMessage = "Email must not exceed 200 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Position is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Position must be between 2 and 100 characters.")]
        public string Position { get; set; } = string.Empty;

        [Required(ErrorMessage = "Join date is required.")]
        public DateTime JoinDate { get; set; }

        [Required(ErrorMessage = "Department ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Department ID must be a positive integer.")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Salary is required.")]
        [Range(0, (double)decimal.MaxValue, ErrorMessage = "Salary cannot be negative.")]
        public decimal Salary { get; set; }

        /// <summary>
        /// Optional: ID đơn ứng tuyển nguồn (luồng Onboarding từ Candidate → Employee).
        /// Nếu có giá trị, Application sẽ được chuyển sang Offered và tài khoản được provision.
        /// </summary>
        public int? SourceApplicationId { get; set; }
    }

    // === DTO cho cập nhật Employee ===
    public class EmployeeUpdateDto
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 200 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(200, ErrorMessage = "Email must not exceed 200 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Position is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Position must be between 2 and 100 characters.")]
        public string Position { get; set; } = string.Empty;

        [Required(ErrorMessage = "Join date is required.")]
        public DateTime JoinDate { get; set; }

        [Required(ErrorMessage = "Department ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Department ID must be a positive integer.")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Salary is required.")]
        [Range(0, (double)decimal.MaxValue, ErrorMessage = "Salary cannot be negative.")]
        public decimal Salary { get; set; }
    }

    // === DTO trả về cho client — Projection từ Employee entity ===
    // Không trả entity thô ra ngoài, chỉ trả DTO (tách biệt domain model vs API contract)
    public class EmployeeResponseDto
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public DateTime JoinDate { get; set; }
        public decimal Salary { get; set; }
        public string Status { get; set; } = string.Empty; // Enum → string projection
        public string DepartmentName { get; set; } = string.Empty; // Join projection từ Department
        public DateTime CreatedAt { get; set; }
    }

    // === DTO cho thống kê nhân viên theo phòng ban — LINQ GroupBy projection ===
    public class EmployeeStatisticsDto
    {
        public string DepartmentName { get; set; } = string.Empty;
        public int EmployeeCount { get; set; }
        public decimal AverageSalary { get; set; }
        public decimal MaxSalary { get; set; }
        public decimal MinSalary { get; set; }
    }

    /// <summary>
    /// Response DTO cho POST /api/employees — bao gồm InitialPassword (chỉ khi NewAccount)
    /// và EmailSent flag để Admin biết email đã gửi thành công hay chưa.
    /// </summary>
    public class EmployeeCreateResponseDto : EmployeeResponseDto
    {
        /// <summary>
        /// Mật khẩu ban đầu — CHỈ trả về 1 lần duy nhất khi tạo tài khoản mới (Kind=NewAccount).
        /// null nếu tài khoản đã tồn tại (Upgraded/Linked).
        /// </summary>
        public string? InitialPassword { get; set; }

        /// <summary>
        /// true nếu email thông báo đã gửi thành công, false nếu SMTP lỗi.
        /// Admin cần thông báo thủ công cho nhân viên nếu false.
        /// </summary>
        public bool EmailSent { get; set; }
    }

    /// <summary>
    /// Response DTO trả về danh sách đồng nghiệp cùng phòng ban (ẩn các thông tin nhạy cảm: Salary, Status).
    /// </summary>
    public class EmployeeColleagueDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}

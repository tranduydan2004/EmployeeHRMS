using System.ComponentModel.DataAnnotations;

namespace EmployeeHRMS.Api.DTOs
{
    // === DTO cho tạo mới Candidate ===
    public class CandidateCreateDto
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 200 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(200, ErrorMessage = "Email must not exceed 200 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [StringLength(20, ErrorMessage = "Phone must not exceed 20 characters.")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Resume URL must not exceed 500 characters.")]
        public string? ResumeUrl { get; set; } = string.Empty;
    }

    // === DTO cho cập nhật Candidate ===
    public class CandidateUpdateDto
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 200 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(200, ErrorMessage = "Email must not exceed 200 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [StringLength(20, ErrorMessage = "Phone must not exceed 20 characters.")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Resume URL must not exceed 500 characters.")]
        public string? ResumeUrl { get; set; } = string.Empty;
    }

    // === DTO trả về — Projection kèm ApplicationCount ===
    public class CandidateResponseDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string ResumeUrl { get; set; } = string.Empty;
        public int ApplicationCount { get; set; }
    }
}

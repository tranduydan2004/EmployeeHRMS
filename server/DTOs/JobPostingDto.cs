using System.ComponentModel.DataAnnotations;
using EmployeeHRMS.Api.DTOs.Common;
using EmployeeHRMS.Api.Models;

namespace EmployeeHRMS.Api.DTOs
{
    // === Query parameters cho JobPosting (Phân trang + Search + Filter) ===
    public class JobPostingQueryParams : PaginationParams
    {
        public int? DepartmentId { get; set; }
        public JobPostingStatus? Status { get; set; }
    }
    // === DTO cho tạo mới JobPosting ===
    public class JobPostingCreateDto
    {
        [Required(ErrorMessage = "Job title is required.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Job title must be between 2 and 200 characters.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(5000, ErrorMessage = "Description must not exceed 5000 characters.")]
        public string? Description { get; set; }

        [StringLength(5000, ErrorMessage = "Requirements must not exceed 5000 characters.")]
        public string? Requirements { get; set; }

        [Required(ErrorMessage = "Department ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Department ID must be a positive integer.")]
        public int DepartmentId { get; set; }

        public JobPostingStatus? Status { get; set; } = JobPostingStatus.Draft;
    }

    // === DTO cho cập nhật JobPosting ===
    public class JobPostingUpdateDto
    {
        [Required(ErrorMessage = "Job title is required.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Job title must be between 2 and 200 characters.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(5000, ErrorMessage = "Description must not exceed 5000 characters.")]
        public string? Description { get; set; }

        [StringLength(5000, ErrorMessage = "Requirements must not exceed 5000 characters.")]
        public string? Requirements { get; set; }

        [Required(ErrorMessage = "Department ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Department ID must be a positive integer.")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        public JobPostingStatus Status { get; set; }
    }

    // === DTO trả về — Projection kèm DepartmentName + ApplicationCount ===
    public class JobPostingResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Requirements { get; set; }
        public string Status { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public int ApplicationCount { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}

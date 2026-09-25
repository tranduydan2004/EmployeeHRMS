using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EmployeeHRMS.Api.Models;
using EmployeeHRMS.Api.Models.ValueObjects;

namespace EmployeeHRMS.Api.DTOs
{
    // ============================================================
    // DTOs cho Phase 1: Smart JD & Question Bank Generation
    // ============================================================

    // === DTO nhận tham số cấu trúc từ HR để sinh bản thảo JD ===
    public class DraftJdRequestDto
    {
        [Required(ErrorMessage = "Job title is required.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Job title must be between 2 and 200 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Department ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Department ID must be a positive integer.")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Level is required.")]
        public JobLevel Level { get; set; }

        [Required(ErrorMessage = "Work mode is required.")]
        public WorkMode WorkMode { get; set; }

        [Required(ErrorMessage = "At least one core skill is required.")]
        [MinLength(1, ErrorMessage = "At least one core skill is required.")]
        public List<string> CoreSkills { get; set; } = new();

        [Range(0, 50, ErrorMessage = "Years of experience must be between 0 and 50.")]
        public int? YearsOfExperience { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Salary min must be a non-negative number.")]
        public decimal? SalaryMin { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Salary max must be a non-negative number.")]
        public decimal? SalaryMax { get; set; }

        public Currency Currency { get; set; } = Currency.VND;

        [StringLength(2000, ErrorMessage = "Additional notes must not exceed 2000 characters.")]
        public string? AdditionalNotes { get; set; }

        public List<string> Certifications { get; set; } = new();
    }

    // === DTO cập nhật nội dung JdContent sau khi HR chỉnh sửa bản thảo ===
    public class UpdateJdContentDto
    {
        [Required(ErrorMessage = "Intro is required.")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Intro must be between 10 and 2000 characters.")]
        public string Intro { get; set; } = string.Empty;

        [Required(ErrorMessage = "Responsibilities are required.")]
        [MinLength(1, ErrorMessage = "At least one responsibility is required.")]
        public List<string> Responsibilities { get; set; } = new();

        [Required(ErrorMessage = "Must-have requirements are required.")]
        [MinLength(1, ErrorMessage = "At least one must-have requirement is required.")]
        public List<string> MustHave { get; set; } = new();

        public List<string> NiceToHave { get; set; } = new();

        public List<string> Benefits { get; set; } = new();
    }

    // === DTO response chi tiết cho JobPosting (bao gồm JdContent + metadata Phase 1) ===
    public class JobPostingDetailResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public int DepartmentId { get; set; }

        // Tham số cấu trúc do HR nhập
        public string Level { get; set; } = string.Empty;
        public string WorkMode { get; set; } = string.Empty;
        public List<string> CoreSkills { get; set; } = new();
        public int? YearsOfExperience { get; set; }
        public SalaryRangeDto? SalaryRange { get; set; }
        public string? AdditionalNotes { get; set; }
        public List<string> Certifications { get; set; } = new();

        // Output từ LLM
        public JdContentDto? JdContent { get; set; }

        // Audit
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ApprovedAt { get; set; }

        // Aggregation
        public int ApplicationCount { get; set; }
        public int QuestionCount { get; set; }
    }

    // === Sub-DTO cho SalaryRange ===
    public class SalaryRangeDto
    {
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public string Currency { get; set; } = "VND";
    }

    // === Sub-DTO cho JdContent ===
    public class JdContentDto
    {
        public string Intro { get; set; } = string.Empty;
        public List<string> Responsibilities { get; set; } = new();
        public List<string> MustHave { get; set; } = new();
        public List<string> NiceToHave { get; set; } = new();
        public List<string> Benefits { get; set; } = new();
    }

    // === DTO response cho QuestionBankItem ===
    public class QuestionBankItemResponseDto
    {
        public int Id { get; set; }
        public int JobPostingId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public ScoringRubricDto? ScoringRubric { get; set; }
        public int OrderIndex { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    // === Sub-DTO cho ScoringRubric ===
    public class ScoringRubricDto
    {
        public string Excellent { get; set; } = string.Empty;
        public string Good { get; set; } = string.Empty;
        public string Acceptable { get; set; } = string.Empty;
        public string Poor { get; set; } = string.Empty;
    }
}

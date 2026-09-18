using System.ComponentModel.DataAnnotations;
using EmployeeHRMS.Api.Models;

namespace EmployeeHRMS.Api.DTOs
{
    // === DTO cho tạo mới Application (đơn ứng tuyển) ===
    public class ApplicationCreateDto
    {
        [Required(ErrorMessage = "Candidate ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Candidate ID must be a positive integer.")]
        public int CandidateId { get; set; }

        [Required(ErrorMessage = "Job Posting ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Job Posting ID must be a positive integer.")]
        public int JobPostingId { get; set; }
    }

    // === DTO cho cập nhật trạng thái Application ===
    public class ApplicationUpdateStatusDto
    {
        [Required(ErrorMessage = "Status is required.")]
        public ApplicationStatus Status { get; set; }
    }

    // === DTO trả về — Projection kèm CandidateName + JobTitle từ entity liên quan ===
    public class ApplicationResponseDto
    {
        public int Id { get; set; }
        public DateTime AppliedDate { get; set; }
        public string Status { get; set; } = string.Empty;

        // Projected fields từ Candidate và JobPosting
        public string CandidateName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;

        // AI analysis results
        public decimal? AiMatchScore { get; set; }
        public string? AiSummary { get; set; }
        public string? AiPros { get; set; }
        public string? AiCons { get; set; }
    }
}

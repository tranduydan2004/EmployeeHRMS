using System;
using System.Collections.Generic;
using EmployeeHRMS.Api.Models.ValueObjects;

namespace EmployeeHRMS.Api.Models
{
    public enum JobPostingStatus
    {
        Draft = 1,
        Approved = 2,
        Published = 3,
        Closed = 4
    }

    public enum JobLevel
    {
        Intern = 0,
        Fresher = 1,
        Junior = 2,
        Middle = 3,
        Senior = 4,
        Lead = 5
    }

    public enum WorkMode
    {
        Onsite = 0,
        Hybrid = 1,
        Remote = 2
    }

    /// <summary>
    /// Tin tuyển dụng và cấu hình sinh JD thông minh.
    /// </summary>
    public class JobPosting
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;

        // --- Tham số cấu trúc do HR nhập (Input cho LLM prompt) ---
        public JobLevel Level { get; set; } = JobLevel.Junior;
        public WorkMode WorkMode { get; set; } = WorkMode.Onsite;
        public List<string> CoreSkills { get; set; } = new();
        public int? YearsOfExperience { get; set; }
        public SalaryRange? SalaryRange { get; set; }
        public string? AdditionalNotes { get; set; }
        public List<string> Certifications { get; set; } = new();

        // --- Output từ LLM (jsonb qua ToJson() Owned Entity) ---
        public JdContent? JdContent { get; set; }

        // --- Trường cũ: Giữ lại để đảm bảo tương thích ngược, không ghi mới ---
        [Obsolete("Dùng JdContent thay thế, giữ lại cho tương thích ngược")]
        public string? Description { get; set; }

        [Obsolete("Dùng JdContent thay thế, giữ lại cho tương thích ngược")]
        public string? Requirements { get; set; }

        // --- Trạng thái & Audit tracking ---
        public JobPostingStatus Status { get; set; } = JobPostingStatus.Draft;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        // --- Foreign Key & Navigation Properties ---
        public int DepartmentId { get; set; }
        public Department Department { get; set; } = null!;

        public ICollection<Application> Applications { get; set; } = new List<Application>();
        public ICollection<QuestionBankItem> QuestionBankItems { get; set; } = new List<QuestionBankItem>();
    }
}

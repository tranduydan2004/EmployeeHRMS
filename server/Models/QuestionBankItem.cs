using System;
using EmployeeHRMS.Api.Models.ValueObjects;

namespace EmployeeHRMS.Api.Models
{
    public enum QuestionCategory
    {
        Technical = 0,
        Behavioral = 1,
        Situational = 2,
        CulturalFit = 3
    }

    /// <summary>
    /// Entity đại diện cho câu hỏi mẫu trong Question Bank gắn với một JobPosting cụ thể.
    /// Tự động sinh ra khi HR duyệt (Approve) bản thảo JD.
    /// </summary>
    public class QuestionBankItem
    {
        public int Id { get; set; }

        public int JobPostingId { get; set; }
        public JobPosting JobPosting { get; set; } = null!;

        public string Question { get; set; } = string.Empty;
        public QuestionCategory Category { get; set; }
        public JobLevel Difficulty { get; set; }
        public ScoringRubric? ScoringRubric { get; set; }

        public int OrderIndex { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}

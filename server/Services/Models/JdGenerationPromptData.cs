using System.Collections.Generic;
using EmployeeHRMS.Api.Models;
using EmployeeHRMS.Api.Models.ValueObjects;

namespace EmployeeHRMS.Api.Services.Models
{
    /// <summary>
    /// Dữ liệu đầu vào nội bộ dùng để tạo prompt sinh JD cho LLM Service.
    /// Tách biệt khỏi DTO (giao tiếp API) và Entity (lưu DB).
    /// </summary>
    public class JdGenerationPromptData
    {
        public string JobTitle { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public JobLevel Level { get; set; }
        public WorkMode WorkMode { get; set; }
        public List<string> CoreSkills { get; set; } = new();
        public int? YearsOfExperience { get; set; }
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public Currency Currency { get; set; } = Currency.VND;
        public string? AdditionalNotes { get; set; }
        public List<string> Certifications { get; set; } = new();
    }
}

using EmployeeHRMS.Api.Models;
using EmployeeHRMS.Api.Models.ValueObjects;

namespace EmployeeHRMS.Api.Services.Models
{
    /// <summary>
    /// Dữ liệu đầu vào nội bộ dùng để tạo prompt sinh bộ câu hỏi phỏng vấn cho LLM Service.
    /// Dựa trên JD đã được HR phê duyệt (Approved).
    /// </summary>
    public class QuestionBankPromptData
    {
        public string JobTitle { get; set; } = string.Empty;
        public JobLevel Level { get; set; }
        public JdContent JdContent { get; set; } = null!;
    }
}

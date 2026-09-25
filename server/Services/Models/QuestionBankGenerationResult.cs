using EmployeeHRMS.Api.Models;
using EmployeeHRMS.Api.Models.ValueObjects;

namespace EmployeeHRMS.Api.Services.Models
{
    /// <summary>
    /// Kết quả trả về từ LLM Service khi sinh từng câu hỏi phỏng vấn và barem chấm điểm.
    /// </summary>
    public class QuestionBankGenerationResult
    {
        public string Question { get; set; } = string.Empty;
        public QuestionCategory Category { get; set; }
        public JobLevel Difficulty { get; set; }
        public ScoringRubric ScoringRubric { get; set; } = null!;
    }
}

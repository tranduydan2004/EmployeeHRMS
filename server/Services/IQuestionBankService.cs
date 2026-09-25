using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EmployeeHRMS.Api.Services.Models;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Service giao tiếp với LLM để sinh bộ câu hỏi phỏng vấn tình huống
    /// kèm barem chấm điểm ScoringRubric dựa trên JD đã phê duyệt.
    /// </summary>
    public interface IQuestionBankService
    {
        /// <summary>
        /// Gọi LLM sinh 3-5 câu hỏi phỏng vấn tình huống kèm barem chấm điểm 4 mức.
        /// </summary>
        /// <param name="promptData">Dữ liệu JD đã được HR phê duyệt</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>Danh sách câu hỏi và barem chấm điểm tương ứng</returns>
        Task<List<QuestionBankGenerationResult>> GenerateQuestionsAsync(QuestionBankPromptData promptData, CancellationToken ct = default);
    }
}

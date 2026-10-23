using System.Threading;
using System.Threading.Tasks;
using EmployeeHRMS.Api.Services.Models;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Service tương tác với OpenAI API để chấm điểm câu trả lời phỏng vấn theo barem ScoringRubric.
    /// </summary>
    public interface IOpenAiScoringService
    {
        /// <summary>
        /// Gửi nội dung câu hỏi, rubric và transcript tới OpenAI LLM để chấm điểm theo 4 mức.
        /// Thực hiện xác thực schema, retry ngữ nghĩa nếu output lỗi, đối soát trích dẫn và quét telemetry injection.
        /// </summary>
        Task<QuestionScoringResult> ScoreAnswerAsync(QuestionScoringRequest request, CancellationToken ct = default);
    }
}

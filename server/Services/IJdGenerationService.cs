using System.Threading;
using System.Threading.Tasks;
using EmployeeHRMS.Api.Models.ValueObjects;
using EmployeeHRMS.Api.Services.Models;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Service giao tiếp với LLM để sinh bản thảo mô tả công việc (Job Description)
    /// theo schema có cấu trúc 5 phần (Intro, Responsibilities, MustHave, NiceToHave, Benefits).
    /// </summary>
    public interface IJdGenerationService
    {
        /// <summary>
        /// Gọi LLM sinh bản thảo JD từ các tham số cấu trúc do HR cung cấp.
        /// </summary>
        /// <param name="promptData">Tham số cấu trúc (Vị trí, cấp bậc, kỹ năng, lương...)</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>Nội dung JD có cấu trúc JdContent</returns>
        Task<JdContent> GenerateJdAsync(JdGenerationPromptData promptData, CancellationToken ct = default);
    }
}

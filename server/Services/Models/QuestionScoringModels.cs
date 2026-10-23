using System.Collections.Generic;
using EmployeeHRMS.Api.Models;
using EmployeeHRMS.Api.Models.ValueObjects;

namespace EmployeeHRMS.Api.Services.Models
{
    /// <summary>
    /// Dữ liệu đầu vào để gửi tới LLM chấm điểm một câu trả lời theo rubric.
    /// Tuân thủ nguyên tắc: chỉ chứa nội dung câu hỏi, rubric và bản bóc băng,
    /// tuyệt đối không chứa dữ liệu giám sát hay thông tin ứng viên.
    /// </summary>
    public class QuestionScoringRequest
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public QuestionCategory? Category { get; set; }
        public ScoringRubric ScoringRubric { get; set; } = null!;
        public string TranscriptSnapshot { get; set; } = string.Empty;
    }

    /// <summary>
    /// Kết quả đánh giá chi tiết từ LLM chấm điểm theo rubric,
    /// kèm kết quả xác thực trích dẫn và telemetry quét prompt injection.
    /// </summary>
    public class QuestionScoringResult
    {
        public RubricLevel Level { get; set; }
        public decimal Score { get; set; }
        public string Analysis { get; set; } = string.Empty;
        public List<string> EvidenceQuotes { get; set; } = new();
        public List<string> Strengths { get; set; } = new();
        public List<string> Concerns { get; set; } = new();
        public string ModelName { get; set; } = string.Empty;
        public string PromptVersion { get; set; } = string.Empty;
        public EvidenceVerificationResult VerificationResult { get; set; } = new();
        public PromptInjectionScanResult InjectionScanResult { get; set; } = new();
        public int? PromptTokens { get; set; }
        public int? CompletionTokens { get; set; }
    }
}

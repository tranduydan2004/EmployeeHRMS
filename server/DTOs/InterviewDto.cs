using System.ComponentModel.DataAnnotations;

namespace EmployeeHRMS.Api.DTOs
{
    // === DTO cho tạo mới Interview ===
    public class InterviewCreateDto
    {
        [Required(ErrorMessage = "Application ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Application ID must be a positive integer.")]
        public int ApplicationId { get; set; }

        [Required(ErrorMessage = "Scheduled date is required.")]
        public DateTime ScheduledDate { get; set; }

        // Interviewer được gán (nullable — Admin/HR gán khi tạo lịch phỏng vấn)
        public int? InterviewerId { get; set; }
    }

    // === DTO cho thêm câu hỏi phỏng vấn ===
    public class InterviewQuestionCreateDto
    {
        [Required(ErrorMessage = "Question is required.")]
        [StringLength(2000, MinimumLength = 5, ErrorMessage = "Question must be between 5 and 2000 characters.")]
        public string Question { get; set; } = string.Empty;

        [Required(ErrorMessage = "Order index is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Order index must be a non-negative integer.")]
        public int OrderIndex { get; set; }
    }

    // === DTO trả về Interview — kèm danh sách câu hỏi (nested projection) ===
    public class InterviewResponseDto
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public int? InterviewerId { get; set; }
        public string? InterviewerName { get; set; }
        public DateTime ScheduledDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal? AiOverallScore { get; set; }
        public string? AiOverallSummary { get; set; }

        // Nested projection: List<InterviewQuestion> → List<InterviewQuestionResponseDto>
        public List<InterviewQuestionResponseDto> Questions { get; set; } = new();
    }

    // === DTO cập nhật câu trả lời phỏng vấn ===
    public class InterviewQuestionAnswerDto
    {
        [Required(ErrorMessage = "Candidate answer is required.")]
        [StringLength(2000, ErrorMessage = "Candidate answer must not exceed 2000 characters.")]
        public string CandidateAnswer { get; set; } = string.Empty;
    }

    // === DTO trả về cho từng câu hỏi phỏng vấn ===
    public class InterviewQuestionResponseDto
    {
        public int Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public string? CandidateAnswer { get; set; }
        public decimal? AiQuestionScore { get; set; }
        public string? AiFeedback { get; set; }
        public int OrderIndex { get; set; }
    }

    // === DTO hoàn tất buổi phỏng vấn ===
    public class InterviewCompleteDto
    {
        [StringLength(4000, ErrorMessage = "Summary must not exceed 4000 characters.")]
        public string? Summary { get; set; }
    }
}

using EmployeeHRMS.Api.Models.ValueObjects;

namespace EmployeeHRMS.Api.Models
{
    public class InterviewQuestion
    {
        public int Id { get; set; }
        public int InterviewId { get; set; }
        public Interview Interview { get; set; } = null!;

        public string Question { get; set; } = string.Empty;
        public string? CandidateAnswer { get; set; }  // Transcript từ Voicebot/Chatbot

        // Đánh giá riêng cho câu này theo rubric
        public decimal? AiQuestionScore { get; set; }
        public string? AiFeedback { get; set; } // Nhận xét AI cho câu trả lời này

        public int OrderIndex { get; set; } // Thứ tự câu hỏi trong buổi phỏng vấn 

        // Snapshot từ QuestionBankItem lúc phát link mời (null với câu hỏi HR thêm thủ công).
        // ScoringRubric phải là instance mới — EF Core không cho 2 owner dùng chung 1 owned instance.
        public ScoringRubric? ScoringRubric { get; set; }
        public int? ThinkSeconds { get; set; }
        public int? AnswerSeconds { get; set; }
        
        // 1-to-1: Mỗi câu hỏi chỉ có tối đa 1 audio response
        public InterviewAnswer? Answer { get; set; }
    }
}

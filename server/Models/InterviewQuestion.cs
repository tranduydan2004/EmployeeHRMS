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
    }
}

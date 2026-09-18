namespace EmployeeHRMS.Api.Models
{

    public enum InterviewStatus
    {
        Scheduled = 1,
        Completed = 2,
        Cancelled = 3
    }

    /// <summary>
    /// Interview entity (AI phỏng vấn sơ loại) — DDD Model.
    /// Status và AI scores được bảo vệ bằng domain methods.
    /// </summary>
    public class Interview
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public Application Application { get; set; } = null!;

        // Interviewer được gán phỏng vấn (nullable — có thể chưa gán)
        public int? InterviewerId { get; set; }
        public ApplicationUser? Interviewer { get; set; }

        public DateTime ScheduledDate { get; set; }

        // DDD: private set — chỉ thay đổi qua domain methods
        public InterviewStatus Status { get; private set; }

        // Điểm & đánh giá tổng hợp (aggregate) sau khi cộng hết các câu
        public decimal? AiOverallScore { get; private set; }
        public string? AiOverallSummary { get; private set; }

        public ICollection<InterviewQuestion> Questions { get; set; } = new List<InterviewQuestion>();

        // ============================================================
        // Domain Methods — Quản lý trạng thái & AI scoring
        // ============================================================

        /// <summary>Thiết lập trạng thái Scheduled khi tạo mới.</summary>
        public void Schedule()
        {
            Status = InterviewStatus.Scheduled;
        }

        /// <summary>Hoàn thành phỏng vấn với summary từ AI.</summary>
        public void Complete(string? summary = null)
        {
            Status = InterviewStatus.Completed;
            AiOverallSummary = summary;
        }

        /// <summary>Hủy buổi phỏng vấn.</summary>
        public void Cancel()
        {
            Status = InterviewStatus.Cancelled;
        }

        /// <summary>
        /// Tính lại AiOverallScore = trung bình điểm các câu hỏi đã có điểm.
        /// Minh họa: LINQ Where + Average aggregate.
        /// </summary>
        public void RecalculateOverallScore()
        {
            var scoredQuestions = Questions.Where(q => q.AiQuestionScore.HasValue);
            AiOverallScore = scoredQuestions.Any()
                ? scoredQuestions.Average(q => q.AiQuestionScore!.Value)
                : null;
        }
    }
}

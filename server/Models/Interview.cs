using EmployeeHRMS.Api.Exceptions;

namespace EmployeeHRMS.Api.Models
{

    public enum InterviewStatus
    {
        Scheduled = 1,
        Completed = 2,
        Cancelled = 3,
        // Audio interview (Phase 2) — giá trị nối tiếp, không đổi giá trị cũ vì cột lưu dạng int
        InProgress = 4,
        Expired = 5
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

        // Token bảo mật dùng cho candidate truy cập buổi phỏng vấn audio (chống IDOR)
        public Guid InviteToken { get; set; } = Guid.NewGuid();

        // Audio interview: hạn dùng link mời (null = phỏng vấn trực tiếp, không qua link)
        public DateTime? InviteExpiresAt { get; private set; }

        // Thời điểm ứng viên đồng ý ghi âm và bắt đầu phiên (set một lần duy nhất)
        public DateTime? ConsentedAt { get; private set; }

        // DDD: private set — chỉ thay đổi qua domain methods
        public InterviewStatus Status { get; private set; }

        // Điểm & đánh giá tổng hợp (aggregate) sau khi cộng hết các câu
        public decimal? AiOverallScore { get; private set; }
        public string? AiOverallSummary { get; private set; }

        /// <summary>
        /// Đề xuất kết quả phỏng vấn (Pass, Consider, Reject) do code quy định dựa trên ngưỡng và knockout rules (Phase 4).
        /// </summary>
        public InterviewRecommendation? Recommendation { get; private set; }

        public ICollection<InterviewQuestion> Questions { get; set; } = new List<InterviewQuestion>();
        public ICollection<InterviewAnswer> Answers { get; set; } = new List<InterviewAnswer>();
        public ICollection<ScreeningDecision> ScreeningDecisions { get; set; } = new List<ScreeningDecision>();
        public ICollection<AudioAccessLog> AudioAccessLogs { get; set; } = new List<AudioAccessLog>();

        // ============================================================
        // Domain Methods — Quản lý trạng thái & AI scoring
        // ============================================================

        /// <summary>Thiết lập trạng thái Scheduled khi tạo mới.</summary>
        public void Schedule()
        {
            Status = InterviewStatus.Scheduled;
        }

        /// <summary>
        /// Hoàn thành phỏng vấn và ghi nhận kết quả tổng hợp.
        /// Theo D4: Complete() chỉ hợp lệ từ InProgress hoặc Expired; gọi lần hai là no-op (idempotent);
        /// ném exception nếu gọi từ Scheduled hoặc Cancelled.
        /// </summary>
        public void Complete(string? summary = null, decimal? overallScore = null, InterviewRecommendation? recommendation = null)
        {
            if (Status == InterviewStatus.Completed)
                return; // Idempotent no-op

            if (Status != InterviewStatus.InProgress && Status != InterviewStatus.Expired)
            {
                throw new BusinessRuleException(
                    $"Cannot complete an interview in '{Status}' state. Only InProgress or Expired interviews can be completed.");
            }

            Status = InterviewStatus.Completed;
            if (summary != null) AiOverallSummary = summary;
            if (overallScore.HasValue) AiOverallScore = overallScore;
            if (recommendation.HasValue) Recommendation = recommendation;
        }

        /// <summary>
        /// Cập nhật kết quả tổng hợp điểm và đề xuất (dùng trong bước aggregation hoặc khi re-evaluate).
        /// </summary>
        public void SetAggregationResult(decimal? overallScore, InterviewRecommendation? recommendation, string? summary = null)
        {
            AiOverallScore = overallScore;
            Recommendation = recommendation;
            if (summary != null)
            {
                AiOverallSummary = summary;
            }
        }

        /// <summary>Hủy buổi phỏng vấn.</summary>
        public void Cancel()
        {
            Status = InterviewStatus.Cancelled;
        }

        /// <summary>
        /// Gán Interviewer phụ trách đánh giá buổi phỏng vấn (Admin/HR thực hiện).
        /// </summary>
        public void AssignInterviewer(int interviewerId)
        {
            if (interviewerId <= 0)
                throw new BusinessRuleException("Interviewer ID must be a positive integer.");

            if (Status == InterviewStatus.Cancelled)
                throw new BusinessRuleException("Cannot assign an interviewer to a cancelled interview.");

            InterviewerId = interviewerId;
        }

        // ============================================================
        // Domain Methods — Audio Interview qua link mời (Phase 2)
        // ============================================================

        /// <summary>
        /// Phát link mời phỏng vấn audio có thời hạn.
        /// ScheduledDate được dùng với nghĩa "ngày mời" để giữ tương thích ngược với luồng HR hiện có.
        /// </summary>
        public void Invite(TimeSpan validFor)
        {
            if (validFor <= TimeSpan.Zero)
                throw new BusinessRuleException("Invite validity must be a positive duration.");

            // Status mặc định (0) = entity mới tạo, chưa qua Schedule()
            if (Status != default && Status != InterviewStatus.Scheduled)
                throw new BusinessRuleException($"Cannot invite an interview in '{Status}' state.");

            var now = DateTime.UtcNow;
            Status = InterviewStatus.Scheduled;
            ScheduledDate = now;
            InviteExpiresAt = now.Add(validFor);
        }

        /// <summary>
        /// Ứng viên đồng ý ghi âm và bắt đầu phiên. Idempotent: gọi lại khi đã InProgress không đổi gì,
        /// ConsentedAt giữ nguyên mốc của lần đầu.
        /// </summary>
        public void MarkInProgress()
        {
            if (Status == InterviewStatus.InProgress)
                return;

            if (Status != InterviewStatus.Scheduled)
                throw new BusinessRuleException($"Cannot start an interview in '{Status}' state.");

            if (IsInviteExpired(DateTime.UtcNow))
                throw new BusinessRuleException("The interview invitation has expired.");

            Status = InterviewStatus.InProgress;
            ConsentedAt ??= DateTime.UtcNow;
        }

        /// <summary>
        /// Đánh dấu link mời đã hết hạn (gọi lazy khi phát hiện quá hạn lúc truy cập). Idempotent.
        /// </summary>
        public void MarkExpired()
        {
            if (Status == InterviewStatus.Expired)
                return;

            if (Status != InterviewStatus.Scheduled && Status != InterviewStatus.InProgress)
                throw new BusinessRuleException($"Cannot expire an interview in '{Status}' state.");

            Status = InterviewStatus.Expired;
        }

        /// <summary>Kiểm tra link mời đã quá hạn tại thời điểm utcNow (không đổi state).</summary>
        public bool IsInviteExpired(DateTime utcNow)
        {
            return InviteExpiresAt.HasValue && utcNow >= InviteExpiresAt.Value;
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

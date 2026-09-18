using EmployeeHRMS.Api.Exceptions;

namespace EmployeeHRMS.Api.Models
{
    public enum ApplicationStatus
    {
        Applied = 1,
        Screening = 2,
        Interviewing = 3,
        Offered = 4,
        Rejected = 5
    }

    /// <summary>
    /// Application entity (Đơn ứng tuyển) — DDD Model.
    /// Status được bảo vệ bằng domain methods TransitionTo() / SetInitialStatus().
    /// State machine: Applied → Screening → Interviewing → Offered/Rejected.
    /// </summary>
    public class Application
    {
        public int Id { get; set; }
        public DateTime AppliedDate { get; set; } = DateTime.UtcNow;

        // DDD: private set — chỉ thay đổi qua domain methods
        public ApplicationStatus Status { get; private set; }

        // Các trường lưu kết quả từ AI (set bởi AI service bên ngoài)
        public decimal? AiMatchScore { get; set; }
        public string? AiSummary { get; set; }
        public string? AiPros { get; set; }
        public string? AiCons { get; set; }

        // Foreign Keys
        public int CandidateId { get; set; }
        public Candidate Candidate { get; set; } = null!;

        public int JobPostingId { get; set; }
        public JobPosting JobPosting { get; set; } = null!;

        // ============================================================
        // Domain Methods — Bảo vệ tính toàn vẹn trạng thái
        // ============================================================

        /// <summary>
        /// Thiết lập trạng thái ban đầu khi tạo đơn ứng tuyển mới.
        /// </summary>
        public void SetInitialStatus()
        {
            Status = ApplicationStatus.Applied;
        }

        /// <summary>
        /// Chuyển trạng thái đơn ứng tuyển theo luồng nghiệp vụ.
        /// Luồng hợp lệ: Applied → Screening → Interviewing → Offered/Rejected.
        /// Có thể Reject ở bất kỳ giai đoạn nào (trừ đã Offered).
        /// </summary>
        public void TransitionTo(ApplicationStatus newStatus)
        {
            if (!IsValidTransition(Status, newStatus))
                throw new BusinessRuleException(
                    $"Invalid status transition: {Status} → {newStatus}. " +
                    $"Valid transitions: Applied→Screening→Interviewing→Offered/Rejected");
            Status = newStatus;
        }

        /// <summary>
        /// Kiểm tra chuyển trạng thái hợp lệ — switch expression (C# 8+).
        /// </summary>
        private static bool IsValidTransition(ApplicationStatus current, ApplicationStatus next)
        {
            return (current, next) switch
            {
                // Luồng chính
                (ApplicationStatus.Applied, ApplicationStatus.Screening) => true,
                (ApplicationStatus.Screening, ApplicationStatus.Interviewing) => true,
                (ApplicationStatus.Interviewing, ApplicationStatus.Offered) => true,

                // Có thể Reject ở bất kỳ giai đoạn nào (trừ đã Offered)
                (ApplicationStatus.Applied, ApplicationStatus.Rejected) => true,
                (ApplicationStatus.Screening, ApplicationStatus.Rejected) => true,
                (ApplicationStatus.Interviewing, ApplicationStatus.Rejected) => true,

                // Tất cả trường hợp khác: không hợp lệ
                _ => false
            };
        }
    }
}

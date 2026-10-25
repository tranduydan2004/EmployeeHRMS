using System;

namespace EmployeeHRMS.Api.Models
{
    /// <summary>
    /// Nhật ký truy cập file ghi âm (Append-only audit trail) ghi nhận ai đã nghe/tải file âm thanh lúc nào.
    /// Đáp ứng yêu cầu bảo vệ dữ liệu cá nhân theo Luật Bảo vệ dữ liệu cá nhân (Luật số 91/2025/QH15).
    /// </summary>
    public class AudioAccessLog
    {
        public int Id { get; set; }

        public int InterviewId { get; set; }
        public Interview Interview { get; set; } = null!;

        public int? InterviewAnswerId { get; set; }
        public InterviewAnswer? InterviewAnswer { get; set; }

        public string StorageKey { get; set; } = string.Empty;

        public int UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;

        public string UserRole { get; set; } = string.Empty;
        public DateTime AccessedAt { get; private set; } = DateTime.UtcNow;
        public string? IpAddress { get; set; }

        public AudioAccessLog() { }

        public static AudioAccessLog Create(
            int interviewId,
            int? interviewAnswerId,
            string storageKey,
            int userId,
            string userRole,
            string? ipAddress)
        {
            return new AudioAccessLog
            {
                InterviewId = interviewId,
                InterviewAnswerId = interviewAnswerId,
                StorageKey = storageKey,
                UserId = userId,
                UserRole = userRole,
                AccessedAt = DateTime.UtcNow,
                IpAddress = ipAddress
            };
        }
    }
}

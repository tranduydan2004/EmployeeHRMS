namespace EmployeeHRMS.Api.Models
{
    /// <summary>
    /// Candidate entity (Ứng viên) — Anemic Model.
    /// EF Core tự sinh Id (DB auto-increment).
    /// </summary>
    public class Candidate
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        // Link lưu trữ file CV (Upload lên Cloud hoặc local folder - AI sẽ đọc file từ link này)
        public string ResumeUrl { get; set; } = string.Empty;

        // Optional link: nếu Candidate có tài khoản đăng nhập
        public int? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public ICollection<Application> Applications { get; set; } = new List<Application>();
    }
}

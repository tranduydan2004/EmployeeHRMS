namespace EmployeeHRMS.Api.Models
{
    public enum UserRole {
        Admin = 1,
        HR = 2,
        Interviewer = 3,
        Candidate = 4,
        Employee = 5
    }
    public class ApplicationUser
    {
        public int Id { get; private set; }
        public string Email { get; private set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;
        public UserRole Role { get; private set; }

        // Optional link: nếu User là nhân viên nội bộ
        public int? EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        // Forgot/Reset Password — lưu plaintext token (sống ngắn 15 phút)
        public string? PasswordResetToken { get; private set; }
        public DateTime? ResetTokenExpiresAt { get; private set; }

        // Email Verification
        public bool IsEmailVerified { get; private set; }
        public string? EmailVerificationTokenHash { get; private set; }
        public DateTime? EmailVerificationTokenExpiresAt { get; private set; }

        // Refresh Tokens navigation
        public List<RefreshToken> RefreshTokens { get; set; } = new();

        // Constructor để khởi tạo hợp lệ ngay từ đầu
        private ApplicationUser() { } // EF Core cần constructor rỗng

        public ApplicationUser(string email, string passwordHash, UserRole role)
        {
            Email = email;
            PasswordHash = passwordHash;
            Role = role;
        }

        // Method có kiểm soát để đổi password - không cho set thẳng
        public void ChangePassword(string newPasswordHash)
        {
            if (string.IsNullOrWhiteSpace(newPasswordHash))
            {
                throw new ArgumentException("Password hash không được rỗng.");
            }
            PasswordHash = newPasswordHash;
        }

        // Method có kiểm soát để đổi role - không cho set thẳng
        public void ChangeRole(UserRole newRole)
        {
            Role = newRole;
        }

        // Method set reset token
        public void SetResetToken(string token, DateTime expiresAt)
        {
            PasswordResetToken = token;
            ResetTokenExpiresAt = expiresAt;
        }

        // Method xóa reset token sau khi đã sử dụng
        public void ClearResetToken()
        {
            PasswordResetToken = null;
            ResetTokenExpiresAt = null;
        }

        // Email Verification domain methods
        public void SetEmailVerificationToken(string tokenHash, DateTime expiresAt)
        {
            EmailVerificationTokenHash = tokenHash;
            EmailVerificationTokenExpiresAt = expiresAt;
        }

        public void MarkEmailVerified()
        {
            IsEmailVerified = true;
            EmailVerificationTokenHash = null;
            EmailVerificationTokenExpiresAt = null;
        }

        public void ClearEmailVerificationToken()
        {
            EmailVerificationTokenHash = null;
            EmailVerificationTokenExpiresAt = null;
        }
    }
}

using System.ComponentModel.DataAnnotations;

namespace EmployeeHRMS.Api.DTOs
{
    /// <summary>
    /// DTO đăng ký tài khoản mới.
    /// KHÔNG có field Role — luôn ép cứng = Candidate trong AuthService.
    /// </summary>
    public class RegisterDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(200, ErrorMessage = "Email must not exceed 200 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters.")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO đăng nhập.
    /// </summary>
    public class LoginDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO trả về sau khi đăng nhập/đăng ký/refresh token thành công.
    /// </summary>
    public class AuthResponseDto
    {
        public int UserId { get; set; }
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiresAt { get; set; }
        public string Role { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO yêu cầu refresh token.
    /// </summary>
    public class RefreshTokenRequestDto
    {
        [Required(ErrorMessage = "Refresh token is required.")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO yêu cầu thu hồi refresh token.
    /// </summary>
    public class RevokeTokenRequestDto
    {
        [Required(ErrorMessage = "Refresh token is required.")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO yêu cầu reset mật khẩu (Bước 1: gửi email chứa link reset).
    /// </summary>
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO đặt lại mật khẩu (Bước 2: submit token + new password).
    /// </summary>
    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Token is required.")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters.")]
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO xác thực email.
    /// </summary>
    public class VerifyEmailDto
    {
        [Required(ErrorMessage = "Token is required.")]
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO yêu cầu gửi lại email xác thực.
    /// </summary>
    public class ResendVerificationDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;
    }
}

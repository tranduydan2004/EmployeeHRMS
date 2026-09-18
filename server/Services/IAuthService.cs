using EmployeeHRMS.Api.DTOs;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Interface cho Authentication Service — Register, Login, Forgot/Reset Password.
    /// </summary>
    public interface IAuthService
    {
        Task RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        Task VerifyEmailAsync(string token);
        Task ResendVerificationAsync(ResendVerificationDto dto);
        Task ForgotPasswordAsync(ForgotPasswordDto dto);
        Task ResetPasswordAsync(ResetPasswordDto dto);
        Task<AuthResponseDto> RefreshTokenAsync(string rawRefreshToken);
        Task RevokeTokenAsync(string rawRefreshToken, int currentUserId);
    }
}

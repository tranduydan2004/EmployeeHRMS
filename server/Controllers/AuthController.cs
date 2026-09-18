using System.Security.Claims;
using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EmployeeHRMS.Api.Controllers
{
    /// <summary>
    /// Auth Controller — Register, Login, Forgot/Reset Password, Refresh & Revoke Token.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")] // Route: api/auth
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            await _authService.RegisterAsync(dto);
            return StatusCode(201, new { message = "Đăng ký thành công. Vui lòng kiểm tra email để xác thực tài khoản trước khi đăng nhập." });
        }

        // GET: api/auth/verify-email?token=...
        [HttpGet("verify-email")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmailGet([FromQuery] string token)
        {
            await _authService.VerifyEmailAsync(token);
            return Ok(new { message = "Email đã được xác thực thành công. Bạn có thể đăng nhập ngay bây giờ." });
        }

        // POST: api/auth/verify-email
        [HttpPost("verify-email")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmailPost([FromBody] VerifyEmailDto dto)
        {
            await _authService.VerifyEmailAsync(dto.Token);
            return Ok(new { message = "Email đã được xác thực thành công. Bạn có thể đăng nhập ngay bây giờ." });
        }

        // POST: api/auth/resend-verification
        // Rate limit: 3 requests/giờ/IP (chống spam email)
        [HttpPost("resend-verification")]
        [AllowAnonymous]
        [EnableRateLimiting("ResendVerificationRateLimit")]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationDto dto)
        {
            await _authService.ResendVerificationAsync(dto);
            return Ok(new { message = "Nếu email tồn tại và chưa xác thực, liên kết xác thực mới đã được gửi." });
        }

        // POST: api/auth/login
        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("AuthRateLimit")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);
            return Ok(result);
        }

        // POST: api/auth/refresh-token
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
        {
            var result = await _authService.RefreshTokenAsync(dto.RefreshToken);
            return Ok(result);
        }

        // POST: api/auth/revoke-token
        [HttpPost("revoke-token")]
        [Authorize]
        public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequestDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var currentUserId))
            {
                return Unauthorized(new { error = "Không thể xác thực người dùng." });
            }

            await _authService.RevokeTokenAsync(dto.RefreshToken, currentUserId);
            return NoContent();
        }

        // POST: api/auth/forgot-password
        // Rate limit riêng: 3 requests/giờ/IP (chống spam email tới nạn nhân)
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [EnableRateLimiting("ForgotPasswordRateLimit")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            await _authService.ForgotPasswordAsync(dto);

            // LUÔN trả 200 OK dù email tồn tại hay không (chống user enumeration)
            return Ok(new { message = "Nếu email tồn tại, link khôi phục đã được gửi." });
        }

        // POST: api/auth/reset-password
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            await _authService.ResetPasswordAsync(dto);
            return Ok(new { message = "Mật khẩu đã được đặt lại thành công." });
        }
    }
}

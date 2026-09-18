using System.Net;
using System.Security.Cryptography;
using EmployeeHRMS.Api.Data;
using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.Exceptions;
using EmployeeHRMS.Api.Helpers;
using EmployeeHRMS.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Auth Service — EF Core implementation.
    /// Register: ép cứng Role = Candidate (không cho client tự chọn).
    /// Login: trả chung 1 message cho cả email sai lẫn password sai (bảo mật).
    /// ForgotPassword: sinh token, gửi email reset link (chống user enumeration).
    /// ResetPassword: verify token, đổi password, xóa token, thu hồi refresh tokens.
    /// RefreshToken: Token Rotation + Replay Detection với SHA-256 hash.
    /// Password hashing: dùng PasswordHasher từ Microsoft.AspNetCore.Identity.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly PasswordHasher<ApplicationUser> _passwordHasher;

        public AuthService(
            AppDbContext context,
            IJwtService jwtService,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
            _passwordHasher = new PasswordHasher<ApplicationUser>();
        }

        public async Task RegisterAsync(RegisterDto dto)
        {
            // Kiểm tra email đã tồn tại
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            if (emailExists)
                throw new BusinessRuleException("Email đã được sử dụng.");

            // Hash password bằng PasswordHasher (bcrypt-like, built-in ASP.NET Core)
            var user = new ApplicationUser(dto.Email, string.Empty, UserRole.Candidate);
            var hashedPassword = _passwordHasher.HashPassword(user, dto.Password);
            user.ChangePassword(hashedPassword);

            // Sinh token xác thực email 64 bytes Base64Url
            var tokenBytes = new byte[64];
            RandomNumberGenerator.Fill(tokenBytes);
            var rawToken = Convert.ToBase64String(tokenBytes)
                .Replace("+", "-").Replace("/", "_").TrimEnd('=');
            var tokenHash = SecurityUtils.HashToken(rawToken);
            var expiresAt = DateTime.UtcNow.AddHours(24);

            user.SetEmailVerificationToken(tokenHash, expiresAt);

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Build verification link và gửi email
            var frontendBaseUrl = _configuration["FrontendBaseUrl"] ?? "http://localhost:5173";
            var verifyLink = $"{frontendBaseUrl}/verify-email?token={Uri.EscapeDataString(rawToken)}";

            var emailSent = await _emailService.SendEmailAsync(
                user.Email,
                "[HRMS] Xác thực địa chỉ email của bạn",
                $"<h2>Xác thực tài khoản HRMS</h2>" +
                $"<p>Chào mừng bạn đến với HRMS. Vui lòng xác thực email bằng cách click vào link bên dưới:</p>" +
                $"<p><a href=\"{verifyLink}\">Xác thực email của tôi</a></p>" +
                $"<p>Link này có hiệu lực trong vòng 24 giờ.</p>" +
                $"<p><em>Nếu bạn không đăng ký tài khoản tại HRMS, vui lòng bỏ qua email này.</em></p>");

            if (!emailSent)
            {
                _logger.LogWarning("Failed to send verification email to {Email}", user.Email);
            }
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            // Tìm user theo email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            // Message chung cho cả email sai lẫn password sai — tránh lộ email nào đã đăng ký
            const string invalidCredentialsMessage = "Email hoặc mật khẩu không đúng.";

            if (user == null)
                throw new BusinessRuleException(invalidCredentialsMessage);

            // Verify password
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
                throw new BusinessRuleException(invalidCredentialsMessage);

            // Kiểm tra trạng thái xác thực email
            if (!user.IsEmailVerified)
            {
                throw new BusinessRuleException("Tài khoản chưa được xác thực email. Vui lòng kiểm tra hộp thư của bạn để kích hoạt tài khoản.", HttpStatusCode.Forbidden);
            }

            // Giới hạn phiên đăng nhập đồng thời (Concurrent Session Capping)
            var maxActiveSessions = int.TryParse(_configuration["JwtSettings:MaxActiveSessionsPerUser"], out var maxSessions)
                ? maxSessions
                : 5;

            var activeTokens = await _context.RefreshTokens
                .Where(t => t.UserId == user.Id && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();

            if (activeTokens.Count >= maxActiveSessions)
            {
                var tokensToRevokeCount = activeTokens.Count - maxActiveSessions + 1;
                var tokensToRevoke = activeTokens.Take(tokensToRevokeCount);
                foreach (var tokenToRevoke in tokensToRevoke)
                {
                    tokenToRevoke.RevokedAt = DateTime.UtcNow;
                    // ReplacedByTokenHash để null để phân biệt với Token Rotation
                }
            }

            // Generate JWT token
            var (accessToken, expiresAt) = _jwtService.GenerateToken(user);
            var rawRefreshToken = await GenerateAndSaveRefreshTokenAsync(user.Id);
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                UserId = user.Id,
                AccessToken = accessToken,
                RefreshToken = rawRefreshToken,
                AccessTokenExpiresAt = expiresAt,
                Role = user.Role.ToString()
            };
        }

        /// <summary>
        /// Xác thực email qua token.
        /// </summary>
        public async Task VerifyEmailAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new BusinessRuleException("Token xác thực không hợp lệ hoặc đã hết hạn.", HttpStatusCode.BadRequest);
            }

            var tokenHash = SecurityUtils.HashToken(token);
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.EmailVerificationTokenHash == tokenHash);

            if (user == null)
            {
                throw new BusinessRuleException("Token xác thực không hợp lệ hoặc đã hết hạn.", HttpStatusCode.BadRequest);
            }

            if (user.EmailVerificationTokenExpiresAt == null || user.EmailVerificationTokenExpiresAt.Value <= DateTime.UtcNow)
            {
                throw new BusinessRuleException("Token xác thực đã hết hạn. Vui lòng yêu cầu gửi lại email xác thực mới.", HttpStatusCode.BadRequest);
            }

            user.MarkEmailVerified();
            await _context.SaveChangesAsync();
            _logger.LogInformation("Email verified successfully for user {Email}", user.Email);
        }

        /// <summary>
        /// Gửi lại email xác thực (chống user enumeration).
        /// </summary>
        public async Task ResendVerificationAsync(ResendVerificationDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            // Chống user enumeration: nếu user không tồn tại hoặc đã xác thực rồi, return bình thường
            if (user == null || user.IsEmailVerified)
            {
                _logger.LogInformation("Resend verification requested for {Email} (user exists: {Exists}, verified: {Verified})",
                    dto.Email, user != null, user?.IsEmailVerified ?? false);
                return;
            }

            var tokenBytes = new byte[64];
            RandomNumberGenerator.Fill(tokenBytes);
            var rawToken = Convert.ToBase64String(tokenBytes)
                .Replace("+", "-").Replace("/", "_").TrimEnd('=');
            var tokenHash = SecurityUtils.HashToken(rawToken);
            var expiresAt = DateTime.UtcNow.AddHours(24);

            user.SetEmailVerificationToken(tokenHash, expiresAt);
            await _context.SaveChangesAsync();

            var frontendBaseUrl = _configuration["FrontendBaseUrl"] ?? "http://localhost:5173";
            var verifyLink = $"{frontendBaseUrl}/verify-email?token={Uri.EscapeDataString(rawToken)}";

            var emailSent = await _emailService.SendEmailAsync(
                user.Email,
                "[HRMS] Xác thực địa chỉ email của bạn",
                $"<h2>Xác thực tài khoản HRMS</h2>" +
                $"<p>Bạn đã yêu cầu gửi lại liên kết xác thực email. Vui lòng click vào link bên dưới:</p>" +
                $"<p><a href=\"{verifyLink}\">Xác thực email của tôi</a></p>" +
                $"<p>Link này có hiệu lực trong vòng 24 giờ.</p>" +
                $"<p><em>Nếu bạn không yêu cầu hành động này, vui lòng bỏ qua email.</em></p>");

            if (!emailSent)
            {
                _logger.LogWarning("Failed to resend verification email to {Email}", user.Email);
            }
        }

        /// <summary>
        /// Refresh Token — Token Rotation & Replay Detection.
        /// </summary>
        public async Task<AuthResponseDto> RefreshTokenAsync(string rawRefreshToken)
        {
            var tokenHash = SecurityUtils.HashToken(rawRefreshToken);
            var token = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

            if (token == null)
            {
                throw new BusinessRuleException("Token không hợp lệ.", HttpStatusCode.Unauthorized);
            }

            // Replay Detection: nếu refresh token đã bị thu hồi trước đó mà vẫn được gửi lên
            if (token.IsRevoked)
            {
                // Phân biệt 2 trường hợp:
                // A. Token bị revoke do rotation (ReplacedByTokenHash != null) -> Tấn công replay thực sự
                if (token.ReplacedByTokenHash != null)
                {
                    // Thu hồi toàn bộ refresh token còn active của user đó (tracked update tương thích InMemory)
                    var activeTokens = await _context.RefreshTokens
                        .Where(rt => rt.UserId == token.UserId && rt.RevokedAt == null)
                        .ToListAsync();

                    foreach (var t in activeTokens)
                    {
                        t.RevokedAt = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync();

                    throw new BusinessRuleException(
                        "Phát hiện truy cập trái phép. Toàn bộ phiên đăng nhập của tài khoản này đã bị thu hồi.",
                        HttpStatusCode.Unauthorized);
                }

                // B. Token bị revoke do Session Capping hoặc Logout (ReplacedByTokenHash == null) -> Không phải replay attack
                throw new BusinessRuleException(
                    "Phiên đăng nhập đã hết hạn hoặc bị đăng xuất từ thiết bị khác. Vui lòng đăng nhập lại.",
                    HttpStatusCode.Unauthorized);
            }

            if (token.IsExpired)
            {
                throw new BusinessRuleException("Refresh token đã hết hạn.", HttpStatusCode.Unauthorized);
            }

            // Transaction bảo đảm không bị mất token giữa chừng (hỗ trợ cả relational DB lẫn InMemory)
            using var transaction = _context.Database.IsRelational()
                ? await _context.Database.BeginTransactionAsync()
                : null;

            var (newAccessToken, newExpiresAt) = _jwtService.GenerateToken(token.User);
            var newRawRefreshToken = _jwtService.GenerateRefreshToken();
            var newHash = SecurityUtils.HashToken(newRawRefreshToken);

            // Đánh dấu token hiện tại đã thu hồi và lưu hash token thay thế
            token.RevokedAt = DateTime.UtcNow;
            token.ReplacedByTokenHash = newHash;

            var expiryDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpiryDays"] ?? "7");
            var newRefreshToken = new RefreshToken
            {
                TokenHash = newHash,
                UserId = token.UserId,
                ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
                CreatedAt = DateTime.UtcNow
            };

            await _context.RefreshTokens.AddAsync(newRefreshToken);
            await _context.SaveChangesAsync();

            if (transaction != null)
            {
                await transaction.CommitAsync();
            }

            return new AuthResponseDto
            {
                UserId = token.UserId,
                AccessToken = newAccessToken,
                RefreshToken = newRawRefreshToken,
                AccessTokenExpiresAt = newExpiresAt,
                Role = token.User.Role.ToString()
            };
        }

        /// <summary>
        /// Revoke Token — người dùng đăng xuất hoặc chủ động thu hồi phiên.
        /// </summary>
        public async Task RevokeTokenAsync(string rawRefreshToken, int currentUserId)
        {
            var tokenHash = SecurityUtils.HashToken(rawRefreshToken);
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

            if (token == null)
            {
                throw new BusinessRuleException("Token không hợp lệ.", HttpStatusCode.Unauthorized);
            }

            if (token.UserId != currentUserId)
            {
                throw new BusinessRuleException("Bạn không có quyền thu hồi token này.", HttpStatusCode.Forbidden);
            }

            if (token.RevokedAt == null)
            {
                token.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Forgot Password — sinh token reset, gửi email.
        /// Dù tìm thấy user hay không, LUÔN return bình thường (chống user enumeration).
        /// </summary>
        public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            if (user == null)
            {
                // Chống user enumeration: không báo lỗi, trả về bình thường
                _logger.LogInformation(
                    "ForgotPassword requested for non-existent email: {Email}", dto.Email);
                return;
            }

            // Sinh token ngẫu nhiên Base64Url
            var tokenBytes = new byte[32];
            RandomNumberGenerator.Fill(tokenBytes);
            var token = Convert.ToBase64String(tokenBytes)
                .Replace("+", "-").Replace("/", "_").TrimEnd('=');

            var expiresAt = DateTime.UtcNow.AddMinutes(15);

            // Lưu plaintext token vào DB
            user.SetResetToken(token, expiresAt);
            await _context.SaveChangesAsync();

            // Build reset link
            var frontendBaseUrl = _configuration["FrontendBaseUrl"] ?? "http://localhost:5173";
            var resetLink = $"{frontendBaseUrl}/reset-password?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";

            // Gửi email SAU khi đã lưu token thành công
            var emailSent = await _emailService.SendEmailAsync(
                user.Email,
                "[HRMS] Yêu cầu đặt lại mật khẩu",
                $"<h2>Đặt lại mật khẩu</h2>" +
                $"<p>Bạn đã yêu cầu đặt lại mật khẩu cho tài khoản <strong>{user.Email}</strong>.</p>" +
                $"<p>Click vào link bên dưới để đặt mật khẩu mới (link hết hạn sau 15 phút):</p>" +
                $"<p><a href=\"{resetLink}\">{resetLink}</a></p>" +
                $"<p><em>Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này.</em></p>");

            if (!emailSent)
            {
                _logger.LogWarning("Failed to send reset password email to {Email}", user.Email);
            }
        }

        /// <summary>
        /// Reset Password — verify token, đổi password, xóa token và thu hồi toàn bộ refresh token.
        /// </summary>
        public async Task ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            // Validate: user tồn tại, token khớp, chưa hết hạn
            if (user == null ||
                user.PasswordResetToken != dto.Token ||
                !user.ResetTokenExpiresAt.HasValue ||
                user.ResetTokenExpiresAt.Value <= DateTime.UtcNow)
            {
                throw new BusinessRuleException(
                    "Token không hợp lệ hoặc đã hết hạn.",
                    HttpStatusCode.Unauthorized);
            }

            // Cập nhật password mới
            var hashedPassword = _passwordHasher.HashPassword(user, dto.NewPassword);
            user.ChangePassword(hashedPassword);

            // Xóa token (đã sử dụng)
            user.ClearResetToken();

            // Thu hồi toàn bộ RefreshToken đang active của user (tracked update, tương thích InMemory)
            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.Id && rt.RevokedAt == null)
                .ToListAsync();

            foreach (var rt in activeTokens)
            {
                rt.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        private async Task<string> GenerateAndSaveRefreshTokenAsync(int userId)
        {
            var rawRefreshToken = _jwtService.GenerateRefreshToken();
            var tokenHash = SecurityUtils.HashToken(rawRefreshToken);
            var expiryDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpiryDays"] ?? "7");

            var refreshToken = new RefreshToken
            {
                TokenHash = tokenHash,
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
                CreatedAt = DateTime.UtcNow
            };

            await _context.RefreshTokens.AddAsync(refreshToken);
            return rawRefreshToken;
        }
    }
}

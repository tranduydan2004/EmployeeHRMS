using System.Net;
using EmployeeHRMS.Api.Data;
using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.Exceptions;
using EmployeeHRMS.Api.Helpers;
using EmployeeHRMS.Api.Models;
using EmployeeHRMS.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace EmployeeHRMS.Tests.Services
{
    public class AuthServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly AuthService _service;
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<ILogger<AuthService>> _loggerMock;

        public AuthServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);

            _jwtServiceMock = new Mock<IJwtService>();
            _jwtServiceMock
                .Setup(j => j.GenerateToken(It.IsAny<ApplicationUser>()))
                .Returns(("fake-jwt-token", DateTime.UtcNow.AddMinutes(15)));

            _jwtServiceMock
                .Setup(j => j.GenerateRefreshToken())
                .Returns(() => Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray()));

            _emailServiceMock = new Mock<IEmailService>();
            _emailServiceMock
                .Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            _configurationMock = new Mock<IConfiguration>();
            _configurationMock.Setup(c => c["JwtSettings:RefreshTokenExpiryDays"]).Returns("7");
            _configurationMock.Setup(c => c["FrontendBaseUrl"]).Returns("http://localhost:5173");

            _loggerMock = new Mock<ILogger<AuthService>>();

            _service = new AuthService(
                _context,
                _jwtServiceMock.Object,
                _emailServiceMock.Object,
                _configurationMock.Object,
                _loggerMock.Object);
        }

        private async Task<AuthResponseDto> RegisterAndLoginUserAsync(string email, string password)
        {
            await _service.RegisterAsync(new RegisterDto
            {
                Email = email,
                Password = password
            });

            var user = await _context.Users.FirstAsync(u => u.Email == email);
            user.MarkEmailVerified();
            await _context.SaveChangesAsync();

            return await _service.LoginAsync(new LoginDto
            {
                Email = email,
                Password = password
            });
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ThrowsBusinessRuleException()
        {
            // Arrange — register a user first
            await _service.RegisterAsync(new RegisterDto
            {
                Email = "user@test.com",
                Password = "Correct@123"
            });

            // Act — login with wrong password
            var act = () => _service.LoginAsync(new LoginDto
            {
                Email = "user@test.com",
                Password = "Wrong@123"
            });

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("Email hoặc mật khẩu không đúng.");
        }

        [Fact]
        public async Task LoginAsync_NonExistentEmail_ThrowsSameMessageAsWrongPassword()
        {
            // Act — login with non-existent email
            var act = () => _service.LoginAsync(new LoginDto
            {
                Email = "nonexistent@test.com",
                Password = "AnyPassword"
            });

            // Assert — same message to prevent user enumeration
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("Email hoặc mật khẩu không đúng.");
        }

        [Fact]
        public async Task RegisterAsync_DuplicateEmail_ThrowsBusinessRuleException()
        {
            // Arrange — register once
            await _service.RegisterAsync(new RegisterDto
            {
                Email = "duplicate@test.com",
                Password = "Pass@123"
            });

            // Act — register again with same email
            var act = () => _service.RegisterAsync(new RegisterDto
            {
                Email = "duplicate@test.com",
                Password = "Pass@456"
            });

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Email*");
        }

        [Fact]
        public async Task RegisterAsync_AlwaysAssignsCandidateRole_RegardlessOfInput()
        {
            // Act — register
            await _service.RegisterAsync(new RegisterDto
            {
                Email = "newuser@test.com",
                Password = "Pass@123"
            });

            // Assert — role should always be Candidate in DB
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "newuser@test.com");
            user.Should().NotBeNull();
            user!.Role.Should().Be(UserRole.Candidate);
        }

        [Fact]
        public async Task RegisterAsync_GeneratesVerificationTokenAndSendsEmail()
        {
            // Act
            await _service.RegisterAsync(new RegisterDto
            {
                Email = "tokenuser@test.com",
                Password = "Password@123"
            });

            // Assert
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "tokenuser@test.com");
            user.Should().NotBeNull();
            user!.EmailVerificationTokenHash.Should().NotBeNullOrWhiteSpace();
            user.IsEmailVerified.Should().BeFalse();

            _emailServiceMock.Verify(e => e.SendEmailAsync(
                "tokenuser@test.com",
                It.Is<string>(s => s.Contains("Xác thực")),
                It.Is<string>(s => s.Contains("verify-email"))), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_ReturnsAccessTokenAndRefreshToken_AndStoresOnlyHashInDb()
        {
            // Arrange
            await _service.RegisterAsync(new RegisterDto
            {
                Email = "loginuser@test.com",
                Password = "Password@123"
            });

            var user = await _context.Users.FirstAsync(u => u.Email == "loginuser@test.com");
            user.MarkEmailVerified();
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.LoginAsync(new LoginDto
            {
                Email = "loginuser@test.com",
                Password = "Password@123"
            });

            // Assert
            result.AccessToken.Should().Be("fake-jwt-token");
            result.RefreshToken.Should().NotBeNullOrWhiteSpace();

            var rawToken = result.RefreshToken;
            var expectedHash = SecurityUtils.HashToken(rawToken);

            // Verify Database: CHỈ lưu hash 64 hex characters, tuyệt đối KHÔNG lưu plaintext token
            var tokenEntity = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == expectedHash);

            tokenEntity.Should().NotBeNull();
            tokenEntity!.TokenHash.Should().HaveLength(64);
            tokenEntity.TokenHash.Should().Be(expectedHash);

            // Plaintext raw token không được xuất hiện trong cột TokenHash
            var rawFoundInDb = await _context.RefreshTokens
                .AnyAsync(rt => rt.TokenHash == rawToken);
            rawFoundInDb.Should().BeFalse();
        }

        [Fact]
        public async Task RefreshTokenAsync_ValidToken_RotatesAndReturnsNewTokens()
        {
            // Arrange
            var authResult = await RegisterAndLoginUserAsync("rotateuser@test.com", "Password@123");
            var initialRefreshToken = authResult.RefreshToken;
            var initialHash = SecurityUtils.HashToken(initialRefreshToken);

            // Act — rotate token
            var refreshResult = await _service.RefreshTokenAsync(initialRefreshToken);

            // Assert
            refreshResult.AccessToken.Should().Be("fake-jwt-token");
            refreshResult.RefreshToken.Should().NotBeNullOrWhiteSpace();
            refreshResult.RefreshToken.Should().NotBe(initialRefreshToken);

            var newHash = SecurityUtils.HashToken(refreshResult.RefreshToken);

            // Verify old token is revoked with ReplacedByTokenHash
            var oldTokenEntity = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == initialHash);

            oldTokenEntity.Should().NotBeNull();
            oldTokenEntity!.RevokedAt.Should().NotBeNull();
            oldTokenEntity.ReplacedByTokenHash.Should().Be(newHash);
            oldTokenEntity.IsActive.Should().BeFalse();

            // Verify new token is active in DB
            var newTokenEntity = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == newHash);

            newTokenEntity.Should().NotBeNull();
            newTokenEntity!.IsActive.Should().BeTrue();
            newTokenEntity.RevokedAt.Should().BeNull();
        }

        [Fact]
        public async Task RefreshTokenAsync_InvalidToken_ThrowsUnauthorized()
        {
            // Act
            var act = () => _service.RefreshTokenAsync("non-existent-raw-token");

            // Assert
            var ex = await act.Should().ThrowAsync<BusinessRuleException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            ex.Which.Message.Should().Contain("Token không hợp lệ");
        }

        [Fact]
        public async Task RefreshTokenAsync_ExpiredToken_ThrowsUnauthorized()
        {
            // Arrange
            var authResult = await RegisterAndLoginUserAsync("expireduser@test.com", "Password@123");
            var rawToken = authResult.RefreshToken;
            var hash = SecurityUtils.HashToken(rawToken);

            // Make it expired manually
            var tokenEntity = await _context.RefreshTokens.FirstAsync(rt => rt.TokenHash == hash);
            tokenEntity.ExpiresAt = DateTime.UtcNow.AddMinutes(-5);
            await _context.SaveChangesAsync();

            // Act
            var act = () => _service.RefreshTokenAsync(rawToken);

            // Assert
            var ex = await act.Should().ThrowAsync<BusinessRuleException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            ex.Which.Message.Should().Contain("Refresh token đã hết hạn");
        }

        [Fact]
        public async Task RefreshTokenAsync_RevokedToken_ReplayDetection_RevokesAllTokensOfUser()
        {
            // Arrange — register user and get token 1
            var authResult = await RegisterAndLoginUserAsync("replayuser@test.com", "Password@123");
            var token1 = authResult.RefreshToken;

            // Rotate once: token1 is now revoked, token2 is active
            var rotatedResult = await _service.RefreshTokenAsync(token1);
            var token2 = rotatedResult.RefreshToken;

            // Also create a second active session (e.g. from another device)
            var loginResult = await _service.LoginAsync(new LoginDto
            {
                Email = "replayuser@test.com",
                Password = "Password@123"
            });
            var token3 = loginResult.RefreshToken;

            // Verify token2 and token3 are currently active
            var user = await _context.Users.FirstAsync(u => u.Email == "replayuser@test.com");
            var activeBeforeReplay = await _context.RefreshTokens
                .CountAsync(rt => rt.UserId == user.Id && rt.RevokedAt == null);
            activeBeforeReplay.Should().Be(2);

            // Act — Attack: Attacker tries to reuse token1 (already revoked)
            var act = () => _service.RefreshTokenAsync(token1);

            // Assert — Replay attack detected
            var ex = await act.Should().ThrowAsync<BusinessRuleException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            ex.Which.Message.Should().Contain("Phát hiện truy cập trái phép");

            // Verify ALL active tokens for this user have been revoked
            var activeAfterReplay = await _context.RefreshTokens
                .CountAsync(rt => rt.UserId == user.Id && rt.RevokedAt == null);
            activeAfterReplay.Should().Be(0);
        }

        [Fact]
        public async Task RevokeTokenAsync_ValidToken_RevokesSuccessfully()
        {
            // Arrange
            var authResult = await RegisterAndLoginUserAsync("revoker@test.com", "Password@123");
            var user = await _context.Users.FirstAsync(u => u.Email == "revoker@test.com");
            var rawToken = authResult.RefreshToken;
            var hash = SecurityUtils.HashToken(rawToken);

            // Act
            await _service.RevokeTokenAsync(rawToken, user.Id);

            // Assert
            var tokenEntity = await _context.RefreshTokens.FirstAsync(rt => rt.TokenHash == hash);
            tokenEntity.IsRevoked.Should().BeTrue();
            tokenEntity.RevokedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task RevokeTokenAsync_WrongUser_ThrowsForbidden()
        {
            // Arrange
            var authResult = await RegisterAndLoginUserAsync("user1@test.com", "Password@123");
            var user1 = await _context.Users.FirstAsync(u => u.Email == "user1@test.com");
            var wrongUserId = user1.Id + 999;

            // Act
            var act = () => _service.RevokeTokenAsync(authResult.RefreshToken, wrongUserId);

            // Assert
            var ex = await act.Should().ThrowAsync<BusinessRuleException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task ResetPasswordAsync_RevokesAllActiveRefreshTokens()
        {
            // Arrange — register user
            var authResult = await RegisterAndLoginUserAsync("resetuser@test.com", "OldPassword@123");

            // Login from second device to have 2 active tokens
            await _service.LoginAsync(new LoginDto
            {
                Email = "resetuser@test.com",
                Password = "OldPassword@123"
            });

            var user = await _context.Users.FirstAsync(u => u.Email == "resetuser@test.com");
            var activeBefore = await _context.RefreshTokens
                .CountAsync(rt => rt.UserId == user.Id && rt.RevokedAt == null);
            activeBefore.Should().Be(2);

            // Set reset token manually
            user.SetResetToken("valid-reset-token", DateTime.UtcNow.AddMinutes(15));
            await _context.SaveChangesAsync();

            // Act — reset password
            await _service.ResetPasswordAsync(new ResetPasswordDto
            {
                Email = "resetuser@test.com",
                Token = "valid-reset-token",
                NewPassword = "NewPassword@123"
            });

            // Assert — all refresh tokens are now revoked
            var activeAfter = await _context.RefreshTokens
                .CountAsync(rt => rt.UserId == user.Id && rt.RevokedAt == null);
            activeAfter.Should().Be(0);

            // Verify password changed
            var actLoginOld = () => _service.LoginAsync(new LoginDto
            {
                Email = "resetuser@test.com",
                Password = "OldPassword@123"
            });
            await actLoginOld.Should().ThrowAsync<BusinessRuleException>();

            var loginNew = await _service.LoginAsync(new LoginDto
            {
                Email = "resetuser@test.com",
                Password = "NewPassword@123"
            });
            loginNew.AccessToken.Should().NotBeNullOrEmpty();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}

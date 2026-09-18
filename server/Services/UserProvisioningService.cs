using System.Security.Cryptography;
using EmployeeHRMS.Api.Data;
using EmployeeHRMS.Api.Exceptions;
using EmployeeHRMS.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Kết quả provision tài khoản — caller (EmployeeService) sẽ dựa vào Kind
    /// để quyết định gửi email nào (hoặc không gửi).
    /// </summary>
    public record ProvisionResult(string Kind, string? PlaintextPassword);

    /// <summary>
    /// UserProvisioningService — Xử lý tạo/nâng cấp/liên kết tài khoản khi onboard Employee.
    /// CHỈ thao tác DB, KHÔNG gửi email (tách khỏi transaction).
    /// </summary>
    public class UserProvisioningService
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<ApplicationUser> _passwordHasher;

        public UserProvisioningService(AppDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<ApplicationUser>();
        }

        /// <summary>
        /// Provision tài khoản Employee — xử lý 3 case theo email.
        /// PHẢI được gọi bên trong transaction (caller quản lý transaction).
        /// </summary>
        public async Task<ProvisionResult> ProvisionEmployeeAccountAsync(string email, int employeeId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            // Case A: Không tồn tại User → Tạo mới
            if (user == null)
            {
                var plainPassword = GenerateSecurePassword(10);
                var newUser = new ApplicationUser(email, string.Empty, UserRole.Employee);
                var hashedPassword = _passwordHasher.HashPassword(newUser, plainPassword);
                newUser.ChangePassword(hashedPassword);
                newUser.EmployeeId = employeeId;
                newUser.MarkEmailVerified();

                await _context.Users.AddAsync(newUser);
                await _context.SaveChangesAsync();

                return new ProvisionResult("NewAccount", plainPassword);
            }

            // Case B: User tồn tại, Role == Candidate → Nâng cấp
            if (user.Role == UserRole.Candidate)
            {
                user.ChangeRole(UserRole.Employee);
                user.EmployeeId = employeeId;
                user.MarkEmailVerified();
                await _context.SaveChangesAsync();

                return new ProvisionResult("Upgraded", null);
            }

            // Case C: User tồn tại, Role khác Candidate (Admin/HR/Interviewer/Employee)
            if (user.EmployeeId.HasValue && user.EmployeeId.Value != employeeId)
            {
                throw new BusinessRuleException("Email này đã gắn với một Employee khác.");
            }

            if (!user.EmployeeId.HasValue)
            {
                user.EmployeeId = employeeId;
                await _context.SaveChangesAsync();
            }

            return new ProvisionResult("Linked", null);
        }

        /// <summary>
        /// Sinh mật khẩu ngẫu nhiên an toàn bằng RandomNumberGenerator.
        /// Đảm bảo có ít nhất 1 chữ hoa, 1 chữ thường, 1 chữ số.
        /// </summary>
        private static string GenerateSecurePassword(int length)
        {
            const string upperChars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lowerChars = "abcdefghjkmnpqrstuvwxyz";
            const string digitChars = "23456789";
            const string allChars = upperChars + lowerChars + digitChars;

            var password = new char[length];
            var randomBytes = new byte[length];
            RandomNumberGenerator.Fill(randomBytes);

            // Đảm bảo ít nhất 1 ký tự mỗi loại
            password[0] = upperChars[randomBytes[0] % upperChars.Length];
            password[1] = lowerChars[randomBytes[1] % lowerChars.Length];
            password[2] = digitChars[randomBytes[2] % digitChars.Length];

            // Phần còn lại random từ tất cả ký tự
            for (int i = 3; i < length; i++)
            {
                password[i] = allChars[randomBytes[i] % allChars.Length];
            }

            // Shuffle để vị trí không cố định
            for (int i = password.Length - 1; i > 0; i--)
            {
                var j = randomBytes[i] % (i + 1);
                (password[i], password[j]) = (password[j], password[i]);
            }

            return new string(password);
        }
    }
}

using Microsoft.AspNetCore.Http;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Service xác thực tính hợp lệ và an toàn của file tải lên (Magic Bytes, MIME, Extension, Structure).
    /// </summary>
    public interface IFileValidatorService
    {
        /// <summary>
        /// Xác thực file CV/Resume theo 4 lớp bảo mật chuyên sâu.
        /// Ném BusinessRuleException nếu file không hợp lệ.
        /// </summary>
        Task ValidateResumeFileAsync(IFormFile file);
    }
}

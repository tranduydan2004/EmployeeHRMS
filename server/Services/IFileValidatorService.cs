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

        /// <summary>
        /// Xác thực file ghi âm phỏng vấn (IFormFile) theo 4 lớp bảo mật (Size, Extension, MIME, Magic Bytes).
        /// </summary>
        Task ValidateAudioFileAsync(IFormFile file);

        /// <summary>
        /// Xác thực stream audio độc lập với IFormFile để hỗ trợ unit test và non-HTTP callers.
        /// </summary>
        Task ValidateAudioStreamAsync(Stream stream, string fileName, string contentType, long length);
    }
}

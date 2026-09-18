namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Interface cho File Storage Service — lưu/xoá file trên local disk.
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// Lưu file resume vào wwwroot/uploads/resumes/, đổi tên bằng GUID.
        /// Trả về relative path (VD: "uploads/resumes/abc123.pdf").
        /// </summary>
        Task<string> SaveResumeAsync(IFormFile file);

        /// <summary>
        /// Xoá file theo relative path.
        /// </summary>
        Task DeleteFileAsync(string relativePath);
    }
}

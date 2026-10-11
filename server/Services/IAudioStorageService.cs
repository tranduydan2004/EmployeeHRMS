using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Abstraction cho tầng lưu trữ audio screening.
    /// Cho phép chuyển đổi linh hoạt giữa Local Disk (dev) và Cloud Blob Storage (S3 / Azure / MinIO).
    /// Hoàn toàn độc lập với ASP.NET Core IFormFile để dùng được cả trong background worker và unit tests.
    /// </summary>
    public interface IAudioStorageService
    {
        /// <summary>
        /// Lưu stream audio vào hệ thống lưu trữ theo storage key đã định.
        /// </summary>
        /// <param name="content">Stream dữ liệu âm thanh</param>
        /// <param name="storageKey">Key định danh vị trí lưu trữ (ví dụ: audio/interviews/1/10_guid.webm)</param>
        /// <param name="contentType">MIME type của audio (ví dụ: audio/webm)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Storage key đã lưu</returns>
        Task<string> SaveAudioAsync(Stream content, string storageKey, string contentType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Đọc stream audio từ hệ thống lưu trữ để forward sang Whisper API hoặc phục vụ playback.
        /// </summary>
        /// <param name="storageKey">Key định danh file audio</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Stream đọc dữ liệu</returns>
        Task<Stream> GetAudioStreamAsync(string storageKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Kiểm tra file audio có tồn tại trong storage hay không.
        /// </summary>
        Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Xóa file audio khi có yêu cầu dọn dẹp hoặc compensating transaction nếu DB insert thất bại.
        /// </summary>
        Task DeleteAudioAsync(string storageKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sinh URL phát lại audio (Presigned URL có hạn đối với Cloud, hoặc endpoint tương đối đối với Local).
        /// </summary>
        Task<string> GetPlaybackUrlAsync(string storageKey, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sinh storage key chuẩn hóa theo quy ước hệ thống.
        /// Định dạng: audio/interviews/{interviewId}/{questionId}_{guid}{extension}
        /// </summary>
        string GenerateStorageKey(int interviewId, int questionId, string extension);
    }
}

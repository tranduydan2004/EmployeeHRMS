using System;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using EmployeeHRMS.Api.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Triển khai lưu trữ audio trên Local Disk cho môi trường dev/demo.
    /// Dữ liệu được lưu trữ ngoài wwwroot (thư mục storage/audio) để bảo mật,
    /// có cơ chế kiểm tra chống Path Traversal và reset con trỏ stream.
    /// </summary>
    public class LocalStorageService : IAudioStorageService
    {
        private readonly string _basePath;
        private readonly ILogger<LocalStorageService> _logger;

        public LocalStorageService(IWebHostEnvironment env, ILogger<LocalStorageService> logger)
        {
            _logger = logger;
            // Lưu tại storage/audio ngoài wwwroot để tránh public static access
            _basePath = Path.Combine(env.ContentRootPath, "storage", "audio");

            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
        }

        public async Task<string> SaveAudioAsync(
            Stream content,
            string storageKey,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            var physicalPath = ResolvePhysicalPath(storageKey);
            var directory = Path.GetDirectoryName(physicalPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Phòng ngừa Stream Position Trap
            if (content.CanSeek)
            {
                content.Position = 0;
            }

            await using (var fileStream = new FileStream(
                physicalPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true))
            {
                await content.CopyToAsync(fileStream, cancellationToken);
            }

            // Rewind stream cho caller tiếp theo nếu cần
            if (content.CanSeek)
            {
                content.Position = 0;
            }

            _logger.LogInformation("Saved audio file: {StorageKey} -> {PhysicalPath}", storageKey, physicalPath);
            return storageKey.Replace('\\', '/');
        }

        public Task<Stream> GetAudioStreamAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            var physicalPath = ResolvePhysicalPath(storageKey);

            if (!File.Exists(physicalPath))
            {
                throw new NotFoundException($"Audio file with key '{storageKey}' not found.");
            }

            Stream stream = new FileStream(
                physicalPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                useAsync: true);

            return Task.FromResult(stream);
        }

        public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            var physicalPath = ResolvePhysicalPath(storageKey);
            return Task.FromResult(File.Exists(physicalPath));
        }

        public Task DeleteAudioAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            var physicalPath = ResolvePhysicalPath(storageKey);

            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
                _logger.LogInformation("Deleted audio file: {StorageKey}", storageKey);
            }

            return Task.CompletedTask;
        }

        public Task<string> GetPlaybackUrlAsync(
            string storageKey,
            TimeSpan? expiry = null,
            CancellationToken cancellationToken = default)
        {
            // Đối với local disk, trả về endpoint stream nội bộ
            var normalizedKey = storageKey.TrimStart('/').Replace('\\', '/');
            return Task.FromResult($"/api/interviews/audio/{Uri.EscapeDataString(normalizedKey)}");
        }

        public string GenerateStorageKey(int interviewId, int questionId, string extension)
        {
            var cleanExtension = string.IsNullOrWhiteSpace(extension)
                ? ".webm"
                : (extension.StartsWith('.') ? extension : $".{extension}").ToLowerInvariant();

            return $"audio/interviews/{interviewId}/{questionId}_{Guid.NewGuid():N}{cleanExtension}";
        }

        /// <summary>
        /// Chuyển đổi storage key thành đường dẫn file vật lý và kiểm tra bảo mật chống Path Traversal.
        /// </summary>
        private string ResolvePhysicalPath(string storageKey)
        {
            if (string.IsNullOrWhiteSpace(storageKey))
            {
                throw new ArgumentException("Storage key cannot be null or whitespace.", nameof(storageKey));
            }

            var normalizedKey = storageKey.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
            var combinedPath = Path.Combine(_basePath, normalizedKey);
            var fullPath = Path.GetFullPath(combinedPath);
            var baseFullPath = Path.GetFullPath(_basePath);

            // Kiểm tra Path Traversal (ngăn chặn ký tự ../ cố tình trỏ ra ngoài _basePath)
            if (!fullPath.StartsWith(baseFullPath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Potential path traversal attack detected: {StorageKey}", storageKey);
                throw new SecurityException("Invalid storage key: Path traversal attempt detected.");
            }

            return fullPath;
        }
    }
}

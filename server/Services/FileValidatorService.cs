using System.IO.Compression;
using EmployeeHRMS.Api.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Service xác thực file tải lên theo quy trình phòng thủ chiều sâu (Defensive-in-Depth 4 lớp).
    /// </summary>
    public class FileValidatorService : IFileValidatorService
    {
        private readonly ILogger<FileValidatorService> _logger;

        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

        private static readonly Dictionary<string, List<byte[]>> FileSignatures = new(StringComparer.OrdinalIgnoreCase)
        {
            { ".pdf", new List<byte[]> { new byte[] { 0x25, 0x50, 0x44, 0x46 } } }, // %PDF
            { ".docx", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } }  // PK.. (ZIP archive)
        };

        private static readonly Dictionary<string, string> StandardMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            { ".pdf", "application/pdf" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" }
        };

        public FileValidatorService(ILogger<FileValidatorService> logger)
        {
            _logger = logger;
        }

        public async Task ValidateResumeFileAsync(IFormFile file)
        {
            // Lớp 1: Null & Size Check
            if (file == null || file.Length == 0)
            {
                throw new BusinessRuleException("Vui lòng chọn file tải lên.");
            }

            if (file.Length > MaxFileSize)
            {
                throw new BusinessRuleException("Dung lượng file không được vượt quá 5MB.");
            }

            // Lớp 2: Extension Whitelist
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!FileSignatures.ContainsKey(ext))
            {
                throw new BusinessRuleException("Chỉ chấp nhận định dạng file .pdf hoặc .docx.");
            }

            // Lớp 3: MIME Type Check (Chỉ cảnh báo qua ILogger, không chặn cứng)
            if (StandardMimeTypes.TryGetValue(ext, out var expectedMime) &&
                !string.Equals(file.ContentType, expectedMime, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("ContentType không khớp chuẩn: {ContentType} cho file {Extension}", file.ContentType, ext);
            }

            // Lớp 4: Magic Bytes Inspection (Cốt lõi, CHẶN CỨNG)
            await using var stream = file.OpenReadStream();
            using var reader = new BinaryReader(stream);

            var maxHeaderLength = FileSignatures[ext].Max(sig => sig.Length);
            var headerBytes = reader.ReadBytes(maxHeaderLength);

            var matchesSignature = FileSignatures[ext].Any(sig =>
                headerBytes.Length >= sig.Length && headerBytes.Take(sig.Length).SequenceEqual(sig));

            if (!matchesSignature)
            {
                throw new BusinessRuleException("Nội dung file không đúng với định dạng khai báo (Giả mạo định dạng).");
            }

            // Kiểm tra sâu cấu trúc .docx (phân biệt .docx thật với các file ZIP thông thường / .xlsx / .pptx)
            if (ext == ".docx")
            {
                stream.Position = 0;
                try
                {
                    using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
                    var hasDocumentXml = archive.Entries.Any(e =>
                        string.Equals(e.FullName, "word/document.xml", StringComparison.OrdinalIgnoreCase));

                    if (!hasDocumentXml)
                    {
                        throw new BusinessRuleException("File .docx không hợp lệ (Thiếu cấu trúc OOXML Word Document).");
                    }
                }
                catch (InvalidDataException)
                {
                    throw new BusinessRuleException("File .docx bị hỏng hoặc không đúng định dạng nén chuẩn.");
                }
            }
        }
    }
}

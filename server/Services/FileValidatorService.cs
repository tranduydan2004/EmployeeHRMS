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
        private const long MaxAudioFileSize = 25 * 1024 * 1024; // 25MB (Whisper API upload limit & đủ cho uncompressed WAV 120s)

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

        private static readonly Dictionary<string, List<string>> AudioMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            { ".webm", new List<string> { "audio/webm", "video/webm" } },
            { ".mp4", new List<string> { "audio/mp4", "video/mp4" } },
            { ".m4a", new List<string> { "audio/mp4", "audio/x-m4a", "audio/m4a" } },
            { ".wav", new List<string> { "audio/wav", "audio/x-wav", "audio/wave" } },
            { ".ogg", new List<string> { "audio/ogg", "application/ogg" } },
            { ".mp3", new List<string> { "audio/mpeg", "audio/mp3" } }
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

        public async Task ValidateAudioFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new BusinessRuleException("Vui lòng tải lên file âm thanh hợp lệ.");
            }

            await using var stream = file.OpenReadStream();
            await ValidateAudioStreamAsync(stream, file.FileName, file.ContentType, file.Length);
        }

        public async Task ValidateAudioStreamAsync(Stream stream, string fileName, string contentType, long length)
        {
            // Lớp 1: Null & Size Check (tối đa 25MB theo hạn mức Whisper API, hỗ trợ WAV uncompressed 60-120s)
            if (stream == null || length <= 0)
            {
                throw new BusinessRuleException("Vui lòng tải lên file âm thanh hợp lệ.");
            }

            if (length > MaxAudioFileSize)
            {
                throw new BusinessRuleException("Dung lượng file âm thanh không được vượt quá 25MB.");
            }

            // Lớp 2: Extension Whitelist
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            if (!AudioMimeTypes.ContainsKey(ext))
            {
                throw new BusinessRuleException("Chỉ chấp nhận các định dạng âm thanh: .webm, .mp4, .m4a, .wav, .ogg, .mp3.");
            }

            // Lớp 3: MIME Type Warning (không block cứng client nếu header bị lệch nhẹ)
            if (AudioMimeTypes.TryGetValue(ext, out var expectedMimes) &&
                !string.IsNullOrWhiteSpace(contentType) &&
                !expectedMimes.Any(m => string.Equals(m, contentType, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Audio ContentType không khớp chuẩn: {ContentType} cho file {Extension}", contentType, ext);
            }

            // Lớp 4: Magic Bytes Inspection (CHẶN CỨNG)
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            var headerBytes = new byte[12];
            var bytesRead = await stream.ReadAsync(headerBytes.AsMemory(0, headerBytes.Length));

            // QUAN TRỌNG: Luôn rewind stream để không bị lỗi Stream Position Trap cho các bước sau
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            if (bytesRead < 4 || !CheckAudioSignature(ext, headerBytes))
            {
                _logger.LogWarning("Phát hiện giả mạo file âm thanh: {FileName}, ext: {Extension}, bytesRead: {BytesRead}",
                    fileName, ext, bytesRead);
                throw new BusinessRuleException("Nội dung file âm thanh không đúng với định dạng khai báo (Giả mạo định dạng).");
            }
        }

        private static bool CheckAudioSignature(string extension, byte[] headerBytes)
        {
            if (headerBytes == null || headerBytes.Length < 4)
            {
                return false;
            }

            switch (extension.ToLowerInvariant())
            {
                case ".webm":
                    // Header EBML: 1A 45 DF A3 tại offset 0
                    return headerBytes.Length >= 4 &&
                           headerBytes[0] == 0x1A && headerBytes[1] == 0x45 &&
                           headerBytes[2] == 0xDF && headerBytes[3] == 0xA3;

                case ".wav":
                    // RIFF tại offset 0 và WAVE tại offset 8
                    return headerBytes.Length >= 12 &&
                           headerBytes[0] == 0x52 && headerBytes[1] == 0x49 &&
                           headerBytes[2] == 0x46 && headerBytes[3] == 0x46 && // RIFF
                           headerBytes[8] == 0x57 && headerBytes[9] == 0x41 &&
                           headerBytes[10] == 0x56 && headerBytes[11] == 0x45; // WAVE

                case ".mp4":
                case ".m4a":
                    // ftyp tại offset 4 (bytes 4..7: 66 74 79 70)
                    return headerBytes.Length >= 8 &&
                           headerBytes[4] == 0x66 && headerBytes[5] == 0x74 &&
                           headerBytes[6] == 0x79 && headerBytes[7] == 0x70;

                case ".ogg":
                    // OggS tại offset 0 (4F 67 67 53)
                    return headerBytes.Length >= 4 &&
                           headerBytes[0] == 0x4F && headerBytes[1] == 0x67 &&
                           headerBytes[2] == 0x67 && headerBytes[3] == 0x53;

                case ".mp3":
                    // ID3 header (49 44 33) hoặc MPEG sync frames (FF FB / FF F3 / FF F2)
                    if (headerBytes.Length >= 3 &&
                        headerBytes[0] == 0x49 && headerBytes[1] == 0x44 && headerBytes[2] == 0x33)
                    {
                        return true;
                    }

                    if (headerBytes.Length >= 2 &&
                        headerBytes[0] == 0xFF &&
                        (headerBytes[1] == 0xFB || headerBytes[1] == 0xF3 || headerBytes[1] == 0xF2))
                    {
                        return true;
                    }

                    return false;

                default:
                    return false;
            }
        }
    }
}

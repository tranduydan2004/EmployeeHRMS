using System.IO.Compression;
using System.Text;
using EmployeeHRMS.Api.Exceptions;
using EmployeeHRMS.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace EmployeeHRMS.Tests.Services
{
    public class FileValidatorServiceTests
    {
        private readonly Mock<ILogger<FileValidatorService>> _mockLogger;
        private readonly FileValidatorService _service;

        public FileValidatorServiceTests()
        {
            _mockLogger = new Mock<ILogger<FileValidatorService>>();
            _service = new FileValidatorService(_mockLogger.Object);
        }

        private static Mock<IFormFile> CreateMockFormFile(byte[] content, string fileName, string contentType)
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.ContentType).Returns(contentType);
            mockFile.Setup(f => f.Length).Returns(content.Length);
            mockFile.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(content));
            return mockFile;
        }

        private static byte[] CreateZipWithEntries(params string[] entryNames)
        {
            using var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var entryName in entryNames)
                {
                    var entry = archive.CreateEntry(entryName);
                    using var entryStream = entry.Open();
                    using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                    writer.Write("<xml>TestContent</xml>");
                }
            }
            return ms.ToArray();
        }

        [Fact]
        public async Task ValidateResume_ValidPdfBytes_DoesNotThrow()
        {
            // Arrange — %PDF-1.4 header
            var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };
            var file = CreateMockFormFile(pdfBytes, "candidate_cv.pdf", "application/pdf");

            // Act
            var act = () => _service.ValidateResumeFileAsync(file.Object);

            // Assert
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task ValidateResume_ValidDocxWithDocumentXmlEntry_DoesNotThrow()
        {
            // Arrange — Gói ZIP chuẩn OpenXML có word/document.xml
            var docxBytes = CreateZipWithEntries("word/document.xml", "[Content_Types].xml");
            var file = CreateMockFormFile(
                docxBytes,
                "resume.docx",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

            // Act
            var act = () => _service.ValidateResumeFileAsync(file.Object);

            // Assert
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task ValidateResume_SpoofedExeRenamedToPdf_ThrowsBusinessRuleException()
        {
            // Arrange — Executable MZ header (0x4D, 0x5A) đổi đuôi thành .pdf
            var exeBytes = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00 };
            var file = CreateMockFormFile(exeBytes, "malware.pdf", "application/pdf");

            // Act
            var act = () => _service.ValidateResumeFileAsync(file.Object);

            // Assert
            var ex = await act.Should().ThrowAsync<BusinessRuleException>();
            ex.WithMessage("*Nội dung file không đúng với định dạng khai báo (Giả mạo định dạng).*");
        }

        [Fact]
        public async Task ValidateResume_FileExceeds5MB_ThrowsBusinessRuleException()
        {
            // Arrange — File vượt quá 5MB
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("heavy_cv.pdf");
            mockFile.Setup(f => f.ContentType).Returns("application/pdf");
            mockFile.Setup(f => f.Length).Returns(5 * 1024 * 1024 + 1);

            // Act
            var act = () => _service.ValidateResumeFileAsync(mockFile.Object);

            // Assert
            var ex = await act.Should().ThrowAsync<BusinessRuleException>();
            ex.WithMessage("*Dung lượng file không được vượt quá 5MB.*");
        }

        [Fact]
        public async Task ValidateResume_NullOrEmptyFile_ThrowsBusinessRuleException()
        {
            // Act 1: null file
            var actNull = () => _service.ValidateResumeFileAsync(null!);
            var exNull = await actNull.Should().ThrowAsync<BusinessRuleException>();
            exNull.WithMessage("*Vui lòng chọn file tải lên.*");

            // Act 2: empty file (Length = 0)
            var emptyFile = CreateMockFormFile(Array.Empty<byte>(), "empty.pdf", "application/pdf");
            var actEmpty = () => _service.ValidateResumeFileAsync(emptyFile.Object);
            var exEmpty = await actEmpty.Should().ThrowAsync<BusinessRuleException>();
            exEmpty.WithMessage("*Vui lòng chọn file tải lên.*");
        }

        [Fact]
        public async Task ValidateResume_InvalidExtension_ThrowsBusinessRuleException()
        {
            // Arrange — Extension không thuộc whitelist (.txt)
            var bytes = Encoding.UTF8.GetBytes("PlainText CV");
            var file = CreateMockFormFile(bytes, "cv.txt", "text/plain");

            // Act
            var act = () => _service.ValidateResumeFileAsync(file.Object);

            // Assert
            var ex = await act.Should().ThrowAsync<BusinessRuleException>();
            ex.WithMessage("*Chỉ chấp nhận định dạng file .pdf hoặc .docx.*");
        }

        [Fact]
        public async Task ValidateResume_MismatchedContentType_DoesNotThrow_OnlyLogsWarning()
        {
            // Arrange — PDF thật nhưng browser gửi application/octet-stream
            var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };
            var file = CreateMockFormFile(pdfBytes, "valid.pdf", "application/octet-stream");

            // Act
            var act = () => _service.ValidateResumeFileAsync(file.Object);

            // Assert — Không throw
            await act.Should().NotThrowAsync();

            // Verify LogLevel.Warning được gọi
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ContentType không khớp chuẩn")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ValidateResume_ZipRenamedToDocx_ThrowsBusinessRuleException()
        {
            // Arrange — File ZIP thông thường (hoặc file .xlsx đổi tên) không chứa word/document.xml
            var zipBytes = CreateZipWithEntries("xl/workbook.xml", "sheet1.xml");
            var file = CreateMockFormFile(
                zipBytes,
                "fake_excel_renamed.docx",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

            // Act
            var act = () => _service.ValidateResumeFileAsync(file.Object);

            // Assert
            var ex = await act.Should().ThrowAsync<BusinessRuleException>();
            ex.WithMessage("*File .docx không hợp lệ (Thiếu cấu trúc OOXML Word Document).*");
        }

        [Fact]
        public async Task ValidateResume_CorruptedZipClaimedAsDocx_ThrowsBusinessRuleException()
        {
            // Arrange — 4 bytes đầu là PK (0x50, 0x4B, 0x03, 0x04) nhưng dữ liệu sau đó bị hỏng
            var corruptedBytes = new byte[]
            {
                0x50, 0x4B, 0x03, 0x04, 0xFF, 0xFE, 0xFD, 0xFC,
                0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77
            };
            var file = CreateMockFormFile(
                corruptedBytes,
                "corrupted.docx",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

            // Act
            var act = () => _service.ValidateResumeFileAsync(file.Object);

            // Assert
            var ex = await act.Should().ThrowAsync<BusinessRuleException>();
            ex.WithMessage("*File .docx bị hỏng hoặc không đúng định dạng nén chuẩn.*");
        }
    }
}

using System.IO;
using System.Threading.Tasks;
using EmployeeHRMS.Api.Exceptions;
using EmployeeHRMS.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EmployeeHRMS.Tests.Services
{
    public class AudioFileValidatorTests
    {
        private readonly FileValidatorService _validator;

        public AudioFileValidatorTests()
        {
            var loggerMock = new Mock<ILogger<FileValidatorService>>();
            _validator = new FileValidatorService(loggerMock.Object);
        }

        [Fact]
        public async Task ValidateAudioStreamAsync_ValidWebm_ShouldSucceed()
        {
            // WebM EBML header: 1A 45 DF A3
            var bytes = new byte[] { 0x1A, 0x45, 0xDF, 0xA3, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
            using var stream = new MemoryStream(bytes);

            var act = () => _validator.ValidateAudioStreamAsync(stream, "answer.webm", "audio/webm", bytes.Length);

            await act.Should().NotThrowAsync();
            stream.Position.Should().Be(0); // Stream position should be rewound
        }

        [Fact]
        public async Task ValidateAudioStreamAsync_ValidWav_ShouldSucceed()
        {
            // WAV: RIFF at offset 0, WAVE at offset 8
            var bytes = new byte[]
            {
                0x52, 0x49, 0x46, 0x46, // RIFF
                0x00, 0x00, 0x00, 0x00, // length
                0x57, 0x41, 0x56, 0x45  // WAVE
            };
            using var stream = new MemoryStream(bytes);

            var act = () => _validator.ValidateAudioStreamAsync(stream, "answer.wav", "audio/wav", bytes.Length);

            await act.Should().NotThrowAsync();
            stream.Position.Should().Be(0);
        }

        [Fact]
        public async Task ValidateAudioStreamAsync_ValidMp4_ShouldSucceed()
        {
            // MP4: ftyp at offset 4 (bytes 4..7: 66 74 79 70)
            var bytes = new byte[]
            {
                0x00, 0x00, 0x00, 0x20, // box size
                0x66, 0x74, 0x79, 0x70, // ftyp
                0x69, 0x73, 0x6F, 0x6D  // isom
            };
            using var stream = new MemoryStream(bytes);

            var act = () => _validator.ValidateAudioStreamAsync(stream, "answer.mp4", "audio/mp4", bytes.Length);

            await act.Should().NotThrowAsync();
            stream.Position.Should().Be(0);
        }

        [Fact]
        public async Task ValidateAudioStreamAsync_ValidOgg_ShouldSucceed()
        {
            // OGG: OggS at offset 0 (4F 67 67 53)
            var bytes = new byte[] { 0x4F, 0x67, 0x67, 0x53, 0x00, 0x02, 0x00, 0x00 };
            using var stream = new MemoryStream(bytes);

            var act = () => _validator.ValidateAudioStreamAsync(stream, "answer.ogg", "audio/ogg", bytes.Length);

            await act.Should().NotThrowAsync();
            stream.Position.Should().Be(0);
        }

        [Fact]
        public async Task ValidateAudioStreamAsync_ValidMp3_WithId3_ShouldSucceed()
        {
            // MP3: ID3 header at offset 0 (49 44 33)
            var bytes = new byte[] { 0x49, 0x44, 0x33, 0x03, 0x00, 0x00, 0x00, 0x00 };
            using var stream = new MemoryStream(bytes);

            var act = () => _validator.ValidateAudioStreamAsync(stream, "answer.mp3", "audio/mpeg", bytes.Length);

            await act.Should().NotThrowAsync();
            stream.Position.Should().Be(0);
        }

        [Fact]
        public async Task ValidateAudioStreamAsync_WhenSizeExceeds25MB_ShouldThrow()
        {
            var bytes = new byte[] { 0x1A, 0x45, 0xDF, 0xA3 };
            using var stream = new MemoryStream(bytes);
            var oversizedLength = 26L * 1024 * 1024; // 26MB

            var act = () => _validator.ValidateAudioStreamAsync(stream, "answer.webm", "audio/webm", oversizedLength);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*không được vượt quá 25MB*");
        }

        [Fact]
        public async Task ValidateAudioStreamAsync_ValidWav_LargeSizeUnder25MB_ShouldSucceed()
        {
            // Uncompressed WAV ~20MB (dưới ngưỡng 25MB) vẫn hợp lệ
            var bytes = new byte[]
            {
                0x52, 0x49, 0x46, 0x46, // RIFF
                0x00, 0x00, 0x00, 0x00, // length
                0x57, 0x41, 0x56, 0x45  // WAVE
            };
            using var stream = new MemoryStream(bytes);
            var largeLength = 20L * 1024 * 1024; // 20MB

            var act = () => _validator.ValidateAudioStreamAsync(stream, "long_answer.wav", "audio/wav", largeLength);

            await act.Should().NotThrowAsync();
            stream.Position.Should().Be(0);
        }

        [Fact]
        public async Task ValidateAudioStreamAsync_WhenExtensionNotWhitelisted_ShouldThrow()
        {
            var bytes = new byte[] { 0x1A, 0x45, 0xDF, 0xA3 };
            using var stream = new MemoryStream(bytes);

            var act = () => _validator.ValidateAudioStreamAsync(stream, "malware.exe", "application/octet-stream", bytes.Length);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Chỉ chấp nhận các định dạng âm thanh*");
        }

        [Fact]
        public async Task ValidateAudioStreamAsync_WhenMagicBytesSpoofed_ShouldThrow()
        {
            // Named .webm but containing plain text / ASCII
            var textBytes = System.Text.Encoding.UTF8.GetBytes("This is not a webm audio file!");
            using var stream = new MemoryStream(textBytes);

            var act = () => _validator.ValidateAudioStreamAsync(stream, "fake.webm", "audio/webm", textBytes.Length);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Giả mạo định dạng*");
        }

        [Fact]
        public async Task ValidateAudioStreamAsync_WhenEmptyStream_ShouldThrow()
        {
            using var stream = new MemoryStream();

            var act = () => _validator.ValidateAudioStreamAsync(stream, "empty.webm", "audio/webm", 0);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Vui lòng tải lên file âm thanh hợp lệ*");
        }
    }
}

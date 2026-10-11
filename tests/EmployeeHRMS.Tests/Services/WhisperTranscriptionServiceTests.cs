using System;
using System.IO;
using System.Threading.Tasks;
using EmployeeHRMS.Api.Configuration;
using EmployeeHRMS.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EmployeeHRMS.Tests.Services
{
    public class WhisperTranscriptionServiceTests
    {
        private readonly Mock<ILogger<WhisperTranscriptionService>> _loggerMock;

        public WhisperTranscriptionServiceTests()
        {
            _loggerMock = new Mock<ILogger<WhisperTranscriptionService>>();
        }

        [Fact]
        public void Constructor_WhenApiKeyMissing_ThrowsInvalidOperationException()
        {
            // Arrange
            var options = Options.Create(new OpenAiSettings
            {
                ApiKey = ""
            });

            // Act
            Action act = () => new WhisperTranscriptionService(options, _loggerMock.Object);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*ApiKey is not configured*");
        }

        [Fact]
        public async Task TranscribeAsync_WhenAudioStreamNull_ThrowsArgumentNullException()
        {
            // Arrange
            var options = Options.Create(new OpenAiSettings
            {
                ApiKey = "fake-sk-key"
            });
            var service = new WhisperTranscriptionService(options, _loggerMock.Object);

            // Act
            Func<Task> act = async () => await service.TranscribeAsync(null!, "test.webm");

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("audioStream");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task TranscribeAsync_WhenFileNameEmpty_ThrowsArgumentException(string? fileName)
        {
            // Arrange
            var options = Options.Create(new OpenAiSettings
            {
                ApiKey = "fake-sk-key"
            });
            var service = new WhisperTranscriptionService(options, _loggerMock.Object);
            using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

            // Act
            Func<Task> act = async () => await service.TranscribeAsync(stream, fileName!);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithParameterName("fileName");
        }
    }
}

using System;
using System.IO;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using EmployeeHRMS.Api.Exceptions;
using EmployeeHRMS.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EmployeeHRMS.Tests.Services
{
    public class LocalStorageServiceTests : IDisposable
    {
        private readonly string _tempDirectory;
        private readonly LocalStorageService _storageService;

        public LocalStorageServiceTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "HRMS_LocalStorageTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);

            var envMock = new Mock<IWebHostEnvironment>();
            envMock.Setup(e => e.ContentRootPath).Returns(_tempDirectory);

            var loggerMock = new Mock<ILogger<LocalStorageService>>();

            _storageService = new LocalStorageService(envMock.Object, loggerMock.Object);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDirectory))
                {
                    Directory.Delete(_tempDirectory, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }

        [Fact]
        public async Task SaveAudioAsync_ShouldSaveFileAndNormalizeKey()
        {
            var contentBytes = Encoding.UTF8.GetBytes("fake audio data");
            using var stream = new MemoryStream(contentBytes);
            var key = "audio/interviews/1/q1_test.webm";

            var savedKey = await _storageService.SaveAudioAsync(stream, key, "audio/webm");

            savedKey.Should().Be("audio/interviews/1/q1_test.webm");
            (await _storageService.ExistsAsync(key)).Should().BeTrue();

            // Stream position should be rewound to 0
            stream.Position.Should().Be(0);
        }

        [Fact]
        public async Task GetAudioStreamAsync_ShouldReturnSavedContent()
        {
            var content = "audio binary content";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var key = "audio/interviews/1/q2_test.webm";

            await _storageService.SaveAudioAsync(stream, key, "audio/webm");

            await using var readStream = await _storageService.GetAudioStreamAsync(key);
            using var reader = new StreamReader(readStream);
            var readContent = await reader.ReadToEndAsync();

            readContent.Should().Be(content);
        }

        [Fact]
        public async Task GetAudioStreamAsync_WhenFileNotFound_ShouldThrowNotFoundException()
        {
            var act = () => _storageService.GetAudioStreamAsync("audio/interviews/99/non_existent.webm");

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task DeleteAudioAsync_ShouldRemoveFile()
        {
            using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            var key = "audio/interviews/1/q3_delete.webm";

            await _storageService.SaveAudioAsync(stream, key, "audio/webm");
            (await _storageService.ExistsAsync(key)).Should().BeTrue();

            await _storageService.DeleteAudioAsync(key);
            (await _storageService.ExistsAsync(key)).Should().BeFalse();
        }

        [Fact]
        public async Task PathTraversal_ShouldThrowSecurityException()
        {
            using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            var maliciousKey = "../../evil.sh";

            var act = () => _storageService.SaveAudioAsync(stream, maliciousKey, "audio/webm");

            await act.Should().ThrowAsync<SecurityException>()
                .WithMessage("*Path traversal attempt detected*");
        }

        [Fact]
        public void GenerateStorageKey_ShouldReturnExpectedFormat()
        {
            var key = _storageService.GenerateStorageKey(42, 5, "webm");

            key.Should().StartWith("audio/interviews/42/5_");
            key.Should().EndWith(".webm");
        }

        [Fact]
        public async Task GetPlaybackUrlAsync_ShouldReturnRelativePlaybackRoute()
        {
            var key = "audio/interviews/10/q1.webm";
            var url = await _storageService.GetPlaybackUrlAsync(key);

            url.Should().Be($"/api/interviews/audio/{Uri.EscapeDataString(key)}");
        }
    }
}

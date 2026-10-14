using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmployeeHRMS.Api.Data;
using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.Exceptions;
using EmployeeHRMS.Api.Models;
using EmployeeHRMS.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EmployeeHRMS.Tests.Services
{
    public class AudioIngestionServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IAudioStorageService> _storageMock;
        private readonly Mock<IFileValidatorService> _validatorMock;
        private readonly Mock<IAudioTranscriptionQueue> _queueMock;
        private readonly Mock<ILogger<AudioIngestionService>> _loggerMock;
        private readonly AudioIngestionService _service;

        private readonly Guid _validInviteToken = Guid.NewGuid();
        private readonly int _validInterviewId = 10;
        private readonly int _validQuestionId = 20;

        public AudioIngestionServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _storageMock = new Mock<IAudioStorageService>();
            _validatorMock = new Mock<IFileValidatorService>();
            _queueMock = new Mock<IAudioTranscriptionQueue>();
            _loggerMock = new Mock<ILogger<AudioIngestionService>>();

            _service = new AudioIngestionService(
                _context,
                _storageMock.Object,
                _validatorMock.Object,
                _queueMock.Object,
                _loggerMock.Object);

            SeedData();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private void SeedData()
        {
            var interview = new Interview
            {
                Id = _validInterviewId,
                ApplicationId = 1,
                InviteToken = _validInviteToken,
                ScheduledDate = DateTime.UtcNow.AddDays(1)
            };
            interview.Schedule();

            var question = new InterviewQuestion
            {
                Id = _validQuestionId,
                InterviewId = _validInterviewId,
                Question = "Tell us about a time you designed a distributed queue.",
                OrderIndex = 1
            };

            _context.Interviews.Add(interview);
            _context.InterviewQuestions.Add(question);
            _context.SaveChanges();
        }

        private static IFormFile CreateMockAudioFormFile(string fileName, string contentType, byte[] content)
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.ContentType).Returns(contentType);
            fileMock.Setup(f => f.Length).Returns(content.Length);
            fileMock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(content));
            return fileMock.Object;
        }

        [Fact]
        public async Task ProcessAudioUploadAsync_ValidRequest_ShouldSaveFile_CreatePendingAnswer_AndEnqueueJob()
        {
            // Arrange
            var audioBytes = new byte[] { 0x1A, 0x45, 0xDF, 0xA3, 0x01, 0x02 }; // WebM header
            var mockFile = CreateMockAudioFormFile("answer.webm", "audio/webm", audioBytes);
            var dto = new AudioAnswerUploadDto
            {
                AudioFile = mockFile,
                DurationSeconds = 45,
                TabSwitchCount = 0,
                OutOfFocusDurationSeconds = 0
            };

            var generatedStorageKey = $"audio/interviews/{_validInterviewId}/{_validQuestionId}_unique.webm";
            _storageMock.Setup(s => s.GenerateStorageKey(_validInterviewId, _validQuestionId, ".webm"))
                .Returns(generatedStorageKey);
            _storageMock.Setup(s => s.SaveAudioAsync(It.IsAny<Stream>(), generatedStorageKey, "audio/webm", It.IsAny<CancellationToken>()))
                .ReturnsAsync(generatedStorageKey);

            // Act
            var (result, isCreated) = await _service.ProcessAudioUploadAsync(_validInviteToken, _validQuestionId, dto);

            // Assert
            isCreated.Should().BeTrue();
            result.Should().NotBeNull();
            result.InterviewQuestionId.Should().Be(_validQuestionId);
            result.AudioStorageKey.Should().Be(generatedStorageKey);
            result.Status.Should().Be("Pending");
            result.AudioDurationSeconds.Should().Be(45);

            // Verify validator was invoked
            _validatorMock.Verify(v => v.ValidateAudioFileAsync(mockFile), Times.Once);

            // Verify saved in DB
            var dbAnswer = await _context.InterviewAnswers.FirstOrDefaultAsync(a => a.InterviewQuestionId == _validQuestionId);
            dbAnswer.Should().NotBeNull();
            dbAnswer!.Status.Should().Be(AudioProcessingStatus.Pending);
            dbAnswer.AudioStorageKey.Should().Be(generatedStorageKey);

            // Verify job was enqueued into Channel
            _queueMock.Verify(q => q.EnqueueAsync(
                It.Is<AudioTranscriptionJob>(j => j.InterviewAnswerId == dbAnswer.Id),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessAudioUploadAsync_WhenInviteTokenNotFound_ShouldThrowNotFoundException()
        {
            // Arrange
            var randomToken = Guid.NewGuid();
            var mockFile = CreateMockAudioFormFile("test.webm", "audio/webm", new byte[] { 1, 2, 3 });
            var dto = new AudioAnswerUploadDto { AudioFile = mockFile, DurationSeconds = 30 };

            // Act
            var act = () => _service.ProcessAudioUploadAsync(randomToken, _validQuestionId, dto);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Không tìm thấy buổi phỏng vấn*");
        }

        [Fact]
        public async Task ProcessAudioUploadAsync_WhenInterviewCancelled_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var interview = await _context.Interviews.FindAsync(_validInterviewId);
            interview!.Cancel();
            await _context.SaveChangesAsync();

            var mockFile = CreateMockAudioFormFile("test.webm", "audio/webm", new byte[] { 1, 2, 3 });
            var dto = new AudioAnswerUploadDto { AudioFile = mockFile, DurationSeconds = 30 };

            // Act
            var act = () => _service.ProcessAudioUploadAsync(_validInviteToken, _validQuestionId, dto);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*đã bị hủy*");
        }

        [Fact]
        public async Task ProcessAudioUploadAsync_WhenInterviewCompleted_ShouldThrowConflictException()
        {
            // Arrange
            var interview = await _context.Interviews.FindAsync(_validInterviewId);
            interview!.Complete();
            await _context.SaveChangesAsync();

            var mockFile = CreateMockAudioFormFile("test.webm", "audio/webm", new byte[] { 1, 2, 3 });
            var dto = new AudioAnswerUploadDto { AudioFile = mockFile, DurationSeconds = 30 };

            // Act
            var act = () => _service.ProcessAudioUploadAsync(_validInviteToken, _validQuestionId, dto);

            // Assert
            await act.Should().ThrowAsync<ConflictException>()
                .WithMessage("*đã hoàn tất*");
        }

        [Fact]
        public async Task ProcessAudioUploadAsync_WhenQuestionNotInInterview_ShouldThrowNotFoundException()
        {
            // Arrange
            var mockFile = CreateMockAudioFormFile("test.webm", "audio/webm", new byte[] { 1, 2, 3 });
            var dto = new AudioAnswerUploadDto { AudioFile = mockFile, DurationSeconds = 30 };
            var wrongQuestionId = 9999;

            // Act
            var act = () => _service.ProcessAudioUploadAsync(_validInviteToken, wrongQuestionId, dto);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*không tồn tại trong buổi phỏng vấn này*");
        }

        [Fact]
        public async Task ProcessAudioUploadAsync_WhenAnswerAlreadyExists_ShouldReturnExistingAnswerIdempotently()
        {
            // Arrange
            var existing = new InterviewAnswer
            {
                InterviewId = _validInterviewId,
                InterviewQuestionId = _validQuestionId,
                AudioStorageKey = "audio/key.webm",
                AudioMimeType = "audio/webm",
                AudioDurationSeconds = 30
            };
            _context.InterviewAnswers.Add(existing);
            await _context.SaveChangesAsync();

            var mockFile = CreateMockAudioFormFile("test.webm", "audio/webm", new byte[] { 1, 2, 3 });
            var dto = new AudioAnswerUploadDto { AudioFile = mockFile, DurationSeconds = 30 };

            // Act
            var (result, isCreated) = await _service.ProcessAudioUploadAsync(_validInviteToken, _validQuestionId, dto);

            // Assert
            isCreated.Should().BeFalse();
            result.Should().NotBeNull();
            result.InterviewQuestionId.Should().Be(_validQuestionId);
            result.AudioStorageKey.Should().Be("audio/key.webm");
            _storageMock.Verify(s => s.SaveAudioAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _queueMock.Verify(q => q.EnqueueAsync(It.IsAny<AudioTranscriptionJob>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ProcessAudioUploadAsync_WhenInterviewExpired_ShouldThrowGoneBusinessRuleException()
        {
            // Arrange
            var interview = await _context.Interviews.FindAsync(_validInterviewId);
            interview!.MarkExpired();
            await _context.SaveChangesAsync();

            var mockFile = CreateMockAudioFormFile("test.webm", "audio/webm", new byte[] { 1, 2, 3 });
            var dto = new AudioAnswerUploadDto { AudioFile = mockFile, DurationSeconds = 30 };

            // Act
            var act = () => _service.ProcessAudioUploadAsync(_validInviteToken, _validQuestionId, dto);

            // Assert
            var ex = await act.Should().ThrowAsync<BusinessRuleException>();
            ex.Which.StatusCode.Should().Be(System.Net.HttpStatusCode.Gone);
        }

        [Fact]
        public async Task ProcessAudioUploadAsync_WithFlags_ShouldWhitelistMicDisconnectedOnly()
        {
            // Arrange
            var mockFile = CreateMockAudioFormFile("test.webm", "audio/webm", new byte[] { 1, 2, 3 });
            var dto = new AudioAnswerUploadDto
            {
                AudioFile = mockFile,
                DurationSeconds = 30,
                Flags = new List<string> { "MicDisconnected", "TabSwitched", "CheatingDetected", "micdisconnected", "InvalidFlag" }
            };

            var generatedStorageKey = $"audio/interviews/{_validInterviewId}/{_validQuestionId}_flags.webm";
            _storageMock.Setup(s => s.GenerateStorageKey(_validInterviewId, _validQuestionId, ".webm"))
                .Returns(generatedStorageKey);
            _storageMock.Setup(s => s.SaveAudioAsync(It.IsAny<Stream>(), generatedStorageKey, "audio/webm", It.IsAny<CancellationToken>()))
                .ReturnsAsync(generatedStorageKey);

            // Act
            var (result, isCreated) = await _service.ProcessAudioUploadAsync(_validInviteToken, _validQuestionId, dto);

            // Assert
            isCreated.Should().BeTrue();
            var dbAnswer = await _context.InterviewAnswers.FirstOrDefaultAsync(a => a.InterviewQuestionId == _validQuestionId);
            dbAnswer.Should().NotBeNull();
            dbAnswer!.Flags.Should().ContainSingle().Which.Should().Be("MicDisconnected");
        }

        [Fact]
        public async Task ProcessAudioUploadAsync_WhenQueueFails_ShouldExecuteCompensatingDelete()
        {
            // Arrange
            var audioBytes = new byte[] { 0x1A, 0x45, 0xDF, 0xA3 };
            var mockFile = CreateMockAudioFormFile("answer.webm", "audio/webm", audioBytes);
            var dto = new AudioAnswerUploadDto { AudioFile = mockFile, DurationSeconds = 30 };

            var storageKey = $"audio/interviews/{_validInterviewId}/{_validQuestionId}_comp.webm";
            _storageMock.Setup(s => s.GenerateStorageKey(_validInterviewId, _validQuestionId, ".webm"))
                .Returns(storageKey);
            _storageMock.Setup(s => s.SaveAudioAsync(It.IsAny<Stream>(), storageKey, "audio/webm", It.IsAny<CancellationToken>()))
                .ReturnsAsync(storageKey);

            // Simulate queue failure
            _queueMock.Setup(q => q.EnqueueAsync(It.IsAny<AudioTranscriptionJob>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Queue channel was closed."));

            // Act
            var act = () => _service.ProcessAudioUploadAsync(_validInviteToken, _validQuestionId, dto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();

            // Verify compensating action deleted the stored audio file
            _storageMock.Verify(s => s.DeleteAudioAsync(storageKey, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAnswerStatusAsync_WhenAnswerExists_ShouldReturnStatusDto()
        {
            // Arrange
            var answer = new InterviewAnswer
            {
                InterviewId = _validInterviewId,
                InterviewQuestionId = _validQuestionId,
                AudioStorageKey = "audio/interviews/10/20.webm",
                AudioMimeType = "audio/webm",
                AudioDurationSeconds = 60,
                AudioFileSize = 1024
            };
            answer.StartTranscribing();
            _context.InterviewAnswers.Add(answer);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetAnswerStatusAsync(_validInviteToken, _validQuestionId);

            // Assert
            result.Should().NotBeNull();
            result.InterviewQuestionId.Should().Be(_validQuestionId);
            result.Status.Should().Be("Transcribing");
        }

        [Fact]
        public async Task GetAnswerStatusAsync_WhenAnswerDoesNotExist_ShouldThrowNotFoundException()
        {
            // Act
            var act = () => _service.GetAnswerStatusAsync(_validInviteToken, _validQuestionId);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Chưa có câu trả lời*");
        }

        [Fact]
        public async Task ProcessAudioUploadAsync_WhenDurationExceedsQuestionLimitPlusBuffer_ShouldThrowBusinessRuleException()
        {
            // Arrange: Câu hỏi có AnswerSeconds = 60, buffer = 5s => tối đa 65s
            var question = await _context.InterviewQuestions.FindAsync(_validQuestionId);
            question!.AnswerSeconds = 60;
            await _context.SaveChangesAsync();

            var audioBytes = new byte[] { 0x1A, 0x45, 0xDF, 0xA3, 0x01, 0x02 };
            var mockFile = CreateMockAudioFormFile("answer.webm", "audio/webm", audioBytes);
            var dto = new AudioAnswerUploadDto
            {
                AudioFile = mockFile,
                DurationSeconds = 66 // Vượt quá 60 + 5 = 65s
            };

            // Act
            var act = () => _service.ProcessAudioUploadAsync(_validInviteToken, _validQuestionId, dto);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*vượt quá giới hạn cho phép*");
        }

        [Fact]
        public async Task ProcessAudioUploadAsync_WhenDurationWithinQuestionLimitPlusBuffer_ShouldSucceed()
        {
            // Arrange: Câu hỏi có AnswerSeconds = 60, buffer = 5s => 65s vẫn hợp lệ
            var question = await _context.InterviewQuestions.FindAsync(_validQuestionId);
            question!.AnswerSeconds = 60;
            await _context.SaveChangesAsync();

            var audioBytes = new byte[] { 0x1A, 0x45, 0xDF, 0xA3, 0x01, 0x02 };
            var mockFile = CreateMockAudioFormFile("answer.webm", "audio/webm", audioBytes);
            var dto = new AudioAnswerUploadDto
            {
                AudioFile = mockFile,
                DurationSeconds = 65 // Đúng ngưỡng 60 + 5 = 65s
            };

            var storageKey = "audio/interviews/10/20_test.webm";
            _storageMock.Setup(s => s.GenerateStorageKey(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(storageKey);
            _storageMock.Setup(s => s.SaveAudioAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(storageKey);

            // Act
            var (response, isCreated) = await _service.ProcessAudioUploadAsync(_validInviteToken, _validQuestionId, dto);

            // Assert
            isCreated.Should().BeTrue();
            response.AudioDurationSeconds.Should().Be(65);
        }

        [Fact]
        public async Task ProcessAudioUploadAsync_WhenQuestionAnswerSecondsIsNull_UsesDefaultAnswerSecondsPlusBuffer()
        {
            // Arrange: Câu hỏi có AnswerSeconds = null => fallback default 90s + 5s buffer = 95s
            var question = await _context.InterviewQuestions.FindAsync(_validQuestionId);
            question!.AnswerSeconds = null;
            await _context.SaveChangesAsync();

            var audioBytes = new byte[] { 0x1A, 0x45, 0xDF, 0xA3, 0x01, 0x02 };
            var mockFile = CreateMockAudioFormFile("answer.webm", "audio/webm", audioBytes);
            var dto = new AudioAnswerUploadDto
            {
                AudioFile = mockFile,
                DurationSeconds = 100 // Vượt quá default 90 + 5 = 95s
            };

            // Act
            var act = () => _service.ProcessAudioUploadAsync(_validInviteToken, _validQuestionId, dto);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*vượt quá giới hạn cho phép*");
        }
    }
}

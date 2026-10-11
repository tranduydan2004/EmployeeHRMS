using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using EmployeeHRMS.Api.Data;
using EmployeeHRMS.Api.Models;
using EmployeeHRMS.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EmployeeHRMS.Tests.Services
{
    public class TranscriptionWorkerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IAudioStorageService> _storageMock;
        private readonly Mock<ISpeechToTextService> _sttMock;
        private readonly Mock<ILogger<TranscriptionWorker>> _loggerMock;
        private readonly Channel<AudioTranscriptionJob> _channel;
        private readonly Mock<IAudioTranscriptionQueue> _queueMock;
        private readonly IServiceProvider _serviceProvider;

        public TranscriptionWorkerTests()
        {
            var dbName = Guid.NewGuid().ToString();
            var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            _context = new AppDbContext(dbOptions);
            _storageMock = new Mock<IAudioStorageService>();
            _sttMock = new Mock<ISpeechToTextService>();
            _loggerMock = new Mock<ILogger<TranscriptionWorker>>();

            _channel = Channel.CreateUnbounded<AudioTranscriptionJob>();
            _queueMock = new Mock<IAudioTranscriptionQueue>();
            _queueMock.Setup(q => q.Reader).Returns(_channel.Reader);

            // Cấu hình ServiceCollection để TranscriptionWorker tạo scope thực tế
            var recoveryMock = new Mock<ITranscriptionRecoveryService>();
            recoveryMock.Setup(r => r.RecoverZombieJobsAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new EmployeeHRMS.Api.DTOs.TranscriptionRecoveryResponseDto());

            var services = new ServiceCollection();
            services.AddScoped(_ => new AppDbContext(dbOptions));
            services.AddScoped(_ => _storageMock.Object);
            services.AddScoped(_ => _sttMock.Object);
            services.AddScoped(_ => recoveryMock.Object);

            _serviceProvider = services.BuildServiceProvider();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task ProcessJob_WhenTranscriptionSucceeds_UpdatesAnswerToScoring()
        {
            // Arrange
            var answer = new InterviewAnswer
            {
                Id = 1,
                InterviewId = 10,
                InterviewQuestionId = 20,
                AudioStorageKey = "audio/interviews/10/20_test.webm",
                AudioDurationSeconds = 45
            };
            _context.InterviewAnswers.Add(answer);
            await _context.SaveChangesAsync();

            var fakeAudioStream = new MemoryStream(Encoding.UTF8.GetBytes("fake audio data"));
            _storageMock
                .Setup(s => s.GetAudioStreamAsync(answer.AudioStorageKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeAudioStream);

            _sttMock
                .Setup(s => s.TranscribeAsync(It.IsAny<Stream>(), "20_test.webm", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TranscriptionResult
                {
                    Text = "Tôi có 3 năm kinh nghiệm lập trình C# và .NET",
                    SegmentsJson = "{\"words\":[{\"word\":\"Tôi\",\"start\":0.1,\"end\":0.3}]}",
                    DetectedLanguage = "vi",
                    DurationSeconds = 45.0
                });

            var worker = new TranscriptionWorker(
                _queueMock.Object,
                _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                _loggerMock.Object);

            // Act: Đẩy job vào channel và đóng channel writer
            await _channel.Writer.WriteAsync(new AudioTranscriptionJob
            {
                InterviewAnswerId = 1,
                EnqueuedAt = DateTime.UtcNow
            });
            _channel.Writer.Complete();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await worker.StartAsync(cts.Token);
            if (worker.ExecuteTask != null)
            {
                await worker.ExecuteTask;
            }

            // Assert
            var updatedAnswer = await _context.InterviewAnswers.AsNoTracking().FirstOrDefaultAsync(a => a.Id == 1);
            updatedAnswer.Should().NotBeNull();
            updatedAnswer!.Status.Should().Be(AudioProcessingStatus.Scoring);
            updatedAnswer.Transcript.Should().Be("Tôi có 3 năm kinh nghiệm lập trình C# và .NET");
            updatedAnswer.TranscriptSegments.Should().Contain("words");
            updatedAnswer.ProcessingStartedAt.Should().NotBeNull();
            updatedAnswer.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public async Task ProcessJob_WhenSttFails_UpdatesAnswerToFailed()
        {
            // Arrange
            var answer = new InterviewAnswer
            {
                Id = 2,
                InterviewId = 10,
                InterviewQuestionId = 21,
                AudioStorageKey = "audio/interviews/10/21_error.webm",
                AudioDurationSeconds = 30
            };
            _context.InterviewAnswers.Add(answer);
            await _context.SaveChangesAsync();

            var fakeAudioStream = new MemoryStream(Encoding.UTF8.GetBytes("fake audio data"));
            _storageMock
                .Setup(s => s.GetAudioStreamAsync(answer.AudioStorageKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeAudioStream);

            _sttMock
                .Setup(s => s.TranscribeAsync(It.IsAny<Stream>(), "21_error.webm", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Whisper API 429 Too Many Requests"));

            var worker = new TranscriptionWorker(
                _queueMock.Object,
                _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                _loggerMock.Object);

            // Act
            await _channel.Writer.WriteAsync(new AudioTranscriptionJob
            {
                InterviewAnswerId = 2,
                EnqueuedAt = DateTime.UtcNow
            });
            _channel.Writer.Complete();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await worker.StartAsync(cts.Token);
            if (worker.ExecuteTask != null)
            {
                await worker.ExecuteTask;
            }

            // Assert
            var updatedAnswer = await _context.InterviewAnswers.AsNoTracking().FirstOrDefaultAsync(a => a.Id == 2);
            updatedAnswer.Should().NotBeNull();
            updatedAnswer!.Status.Should().Be(AudioProcessingStatus.Failed);
            updatedAnswer.ErrorMessage.Should().Contain("Whisper API 429 Too Many Requests");
            updatedAnswer.Transcript.Should().BeNull();
        }

        [Fact]
        public async Task ProcessJob_WhenAnswerNotFound_SkipsGracefullyWithoutCrashing()
        {
            // Arrange: AnswerId = 999 không tồn tại trong DB
            var worker = new TranscriptionWorker(
                _queueMock.Object,
                _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                _loggerMock.Object);

            // Act
            await _channel.Writer.WriteAsync(new AudioTranscriptionJob
            {
                InterviewAnswerId = 999,
                EnqueuedAt = DateTime.UtcNow
            });
            _channel.Writer.Complete();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await worker.StartAsync(cts.Token);
            if (worker.ExecuteTask != null)
            {
                await worker.ExecuteTask;
            }

            // Assert: Không throw exception
            _sttMock.Verify(s => s.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ProcessJob_WhenAnswerAlreadyCompleted_SkipsDuplicateJob()
        {
            // Arrange: Answer đã ở trạng thái Completed qua domain methods
            var answer = new InterviewAnswer
            {
                Id = 3,
                InterviewId = 10,
                InterviewQuestionId = 22,
                AudioStorageKey = "audio/interviews/10/22_completed.webm",
                AudioDurationSeconds = 60
            };
            answer.StartTranscribing();
            answer.CompleteTranscription("Existing transcript");
            answer.CompleteScoring(8.5m, "Good");

            _context.InterviewAnswers.Add(answer);
            await _context.SaveChangesAsync();

            var worker = new TranscriptionWorker(
                _queueMock.Object,
                _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                _loggerMock.Object);

            // Act
            await _channel.Writer.WriteAsync(new AudioTranscriptionJob
            {
                InterviewAnswerId = 3,
                EnqueuedAt = DateTime.UtcNow
            });
            _channel.Writer.Complete();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await worker.StartAsync(cts.Token);
            if (worker.ExecuteTask != null)
            {
                await worker.ExecuteTask;
            }

            // Assert: STT không được gọi, trạng thái vẫn giữ nguyên
            _sttMock.Verify(s => s.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            var unchangedAnswer = await _context.InterviewAnswers.FindAsync(3);
            unchangedAnswer!.Status.Should().Be(AudioProcessingStatus.Completed);
            unchangedAnswer.Transcript.Should().Be("Existing transcript");
        }
    }
}

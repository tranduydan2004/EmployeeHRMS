using System;
using System.Threading;
using System.Threading.Tasks;
using EmployeeHRMS.Api.Data;
using EmployeeHRMS.Api.Models;
using EmployeeHRMS.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EmployeeHRMS.Tests.Services
{
    public class TranscriptionRecoveryServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IAudioTranscriptionQueue> _queueMock;
        private readonly Mock<ILogger<TranscriptionRecoveryService>> _loggerMock;
        private readonly TranscriptionRecoveryService _service;

        public TranscriptionRecoveryServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _queueMock = new Mock<IAudioTranscriptionQueue>();
            _loggerMock = new Mock<ILogger<TranscriptionRecoveryService>>();

            _service = new TranscriptionRecoveryService(
                _context,
                _queueMock.Object,
                _loggerMock.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task RecoverZombieJobsAsync_Startup_WhenZombieUnderMaxRetries_RequeuesToPending()
        {
            // Arrange: 1 câu trả lời đang ở trạng thái Transcribing (zombie do crash)
            var zombie = new InterviewAnswer
            {
                Id = 1,
                InterviewId = 10,
                InterviewQuestionId = 20,
                AudioStorageKey = "audio/interviews/10/20_zombie.webm",
                AudioDurationSeconds = 40
            };
            zombie.StartTranscribing(); // Status = Transcribing, RetryCount = 0
            _context.InterviewAnswers.Add(zombie);
            await _context.SaveChangesAsync();

            // Act: Chạy Startup Recovery
            var result = await _service.RecoverZombieJobsAsync(isStartup: true);

            // Assert
            result.ZombieJobsRecovered.Should().Be(1);
            result.ZombieJobsFailed.Should().Be(0);
            result.PendingJobsRequeued.Should().Be(0);
            result.TotalProcessed.Should().Be(1);

            var updatedZombie = await _context.InterviewAnswers.AsNoTracking().FirstOrDefaultAsync(a => a.Id == 1);
            updatedZombie.Should().NotBeNull();
            updatedZombie!.Status.Should().Be(AudioProcessingStatus.Pending);
            updatedZombie.RetryCount.Should().Be(1);
            updatedZombie.ProcessingStartedAt.Should().BeNull();

            _queueMock.Verify(q => q.EnqueueAsync(
                It.Is<AudioTranscriptionJob>(j => j.InterviewAnswerId == 1),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RecoverZombieJobsAsync_Startup_WhenZombieExceedsMaxRetries_MarksFailed()
        {
            // Arrange: 1 câu trả lời đã retry 3 lần và lại bị kẹt ở Transcribing
            var zombie = new InterviewAnswer
            {
                Id = 2,
                InterviewId = 10,
                InterviewQuestionId = 21,
                AudioStorageKey = "audio/interviews/10/21_zombie_max.webm",
                AudioDurationSeconds = 50
            };
            zombie.StartTranscribing();
            zombie.RequeueForRetry(); // retry 1
            zombie.StartTranscribing();
            zombie.RequeueForRetry(); // retry 2
            zombie.StartTranscribing();
            zombie.RequeueForRetry(); // retry 3
            zombie.StartTranscribing(); // Status = Transcribing, RetryCount = 3

            _context.InterviewAnswers.Add(zombie);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.RecoverZombieJobsAsync(isStartup: true);

            // Assert
            result.ZombieJobsRecovered.Should().Be(0);
            result.ZombieJobsFailed.Should().Be(1);
            result.TotalProcessed.Should().Be(1);

            var updatedZombie = await _context.InterviewAnswers.AsNoTracking().FirstOrDefaultAsync(a => a.Id == 2);
            updatedZombie.Should().NotBeNull();
            updatedZombie!.Status.Should().Be(AudioProcessingStatus.Failed);
            updatedZombie.ErrorMessage.Should().Contain("Đã vượt quá số lần thử lại tối đa (3)");

            // Không được nạp vào hàng đợi vì đã Failed
            _queueMock.Verify(q => q.EnqueueAsync(
                It.Is<AudioTranscriptionJob>(j => j.InterviewAnswerId == 2),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RecoverZombieJobsAsync_Startup_WhenPendingJobsExist_RequeuesAllPendingJobs()
        {
            // Arrange: 1 job Pending (chưa xử lý trước restart) và 1 job Zombie (Transcribing)
            var pendingAnswer = new InterviewAnswer
            {
                Id = 3,
                InterviewId = 10,
                InterviewQuestionId = 22,
                AudioStorageKey = "audio/interviews/10/22_pending.webm",
                AudioDurationSeconds = 30
            }; // Status = Pending

            var zombieAnswer = new InterviewAnswer
            {
                Id = 4,
                InterviewId = 10,
                InterviewQuestionId = 23,
                AudioStorageKey = "audio/interviews/10/23_zombie.webm",
                AudioDurationSeconds = 45
            };
            zombieAnswer.StartTranscribing();

            _context.InterviewAnswers.AddRange(pendingAnswer, zombieAnswer);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.RecoverZombieJobsAsync(isStartup: true);

            // Assert
            result.ZombieJobsRecovered.Should().Be(1);
            result.PendingJobsRequeued.Should().Be(1);
            result.TotalProcessed.Should().Be(2);

            _queueMock.Verify(q => q.EnqueueAsync(
                It.Is<AudioTranscriptionJob>(j => j.InterviewAnswerId == 3),
                It.IsAny<CancellationToken>()), Times.Once);

            _queueMock.Verify(q => q.EnqueueAsync(
                It.Is<AudioTranscriptionJob>(j => j.InterviewAnswerId == 4),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RecoverZombieJobsAsync_Periodic_IgnoresRecentJobs_OnlyRecoversExpiredJobs()
        {
            // Arrange:
            // Job 5: Bắt đầu 5 phút trước (< 15 phút timeout) -> Vẫn đang chạy bình thường, KHÔNG chạm vào
            // Job 6: Bắt đầu 25 phút trước (> 15 phút timeout) -> Zombie treo, CẦN phục hồi
            var recentJob = new InterviewAnswer
            {
                Id = 5,
                InterviewId = 10,
                InterviewQuestionId = 24,
                AudioStorageKey = "audio/interviews/10/24_recent.webm",
                AudioDurationSeconds = 20
            };
            recentJob.StartTranscribing();

            var expiredJob = new InterviewAnswer
            {
                Id = 6,
                InterviewId = 10,
                InterviewQuestionId = 25,
                AudioStorageKey = "audio/interviews/10/25_expired.webm",
                AudioDurationSeconds = 25
            };
            expiredJob.StartTranscribing();

            _context.InterviewAnswers.AddRange(recentJob, expiredJob);
            await _context.SaveChangesAsync();

            // Giả lập thời gian bắt đầu của expiredJob là 25 phút trước
            // Dùng reflection/DbContext entry để cập nhật ProcessingStartedAt cho mục đích test
            _context.Entry(expiredJob).Property(e => e.ProcessingStartedAt).CurrentValue = DateTime.UtcNow.AddMinutes(-25);
            _context.Entry(recentJob).Property(e => e.ProcessingStartedAt).CurrentValue = DateTime.UtcNow.AddMinutes(-5);
            await _context.SaveChangesAsync();

            // Act: Chạy Periodic check (isStartup: false)
            var result = await _service.RecoverZombieJobsAsync(isStartup: false);

            // Assert
            result.ZombieJobsRecovered.Should().Be(1);
            result.PendingJobsRequeued.Should().Be(0);

            var updatedRecent = await _context.InterviewAnswers.AsNoTracking().FirstOrDefaultAsync(a => a.Id == 5);
            updatedRecent!.Status.Should().Be(AudioProcessingStatus.Transcribing); // Giữ nguyên

            var updatedExpired = await _context.InterviewAnswers.AsNoTracking().FirstOrDefaultAsync(a => a.Id == 6);
            updatedExpired!.Status.Should().Be(AudioProcessingStatus.Pending); // Phục hồi

            _queueMock.Verify(q => q.EnqueueAsync(
                It.Is<AudioTranscriptionJob>(j => j.InterviewAnswerId == 6),
                It.IsAny<CancellationToken>()), Times.Once);

            _queueMock.Verify(q => q.EnqueueAsync(
                It.Is<AudioTranscriptionJob>(j => j.InterviewAnswerId == 5),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RecoverZombieJobsAsync_WhenNoJobs_ReturnsZeroCount()
        {
            // Arrange: DB rỗng
            // Act
            var result = await _service.RecoverZombieJobsAsync(isStartup: true);

            // Assert
            result.TotalProcessed.Should().Be(0);
            result.ZombieJobsRecovered.Should().Be(0);
            result.ZombieJobsFailed.Should().Be(0);
            result.PendingJobsRequeued.Should().Be(0);

            _queueMock.Verify(q => q.EnqueueAsync(
                It.IsAny<AudioTranscriptionJob>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}

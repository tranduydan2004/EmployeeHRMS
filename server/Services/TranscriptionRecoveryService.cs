using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmployeeHRMS.Api.Data;
using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Triển khai ITranscriptionRecoveryService:
    /// - Quét DB tìm các InterviewAnswer bị kẹt ở trạng thái Transcribing.
    /// - Nếu RetryCount >= MaxZombieRetries (3), đánh dấu Failed.
    /// - Nếu RetryCount < MaxZombieRetries, gọi RequeueForRetry() và đẩy lại vào Channel queue.
    /// - Khi Startup (isStartup = true), cũng đồng thời nạp lại tất cả các job đang ở trạng thái Pending
    ///   vì in-memory Channel rỗng sau khi server khởi động lại.
    /// </summary>
    public class TranscriptionRecoveryService : ITranscriptionRecoveryService
    {
        private readonly AppDbContext _context;
        private readonly IAudioTranscriptionQueue _queue;
        private readonly ILogger<TranscriptionRecoveryService> _logger;

        public const int MaxZombieRetries = 3;
        public static readonly TimeSpan ZombieTimeout = TimeSpan.FromMinutes(15);

        public TranscriptionRecoveryService(
            AppDbContext context,
            IAudioTranscriptionQueue queue,
            ILogger<TranscriptionRecoveryService> logger)
        {
            _context = context;
            _queue = queue;
            _logger = logger;
        }

        public async Task<TranscriptionRecoveryResponseDto> RecoverZombieJobsAsync(
            bool isStartup = true,
            CancellationToken cancellationToken = default)
        {
            var response = new TranscriptionRecoveryResponseDto();

            _logger.LogInformation(
                "Bắt đầu quá trình Startup Recovery / Zombie Job Scan (isStartup: {IsStartup})...",
                isStartup);

            // 1. Quét các job bị kẹt ở trạng thái Transcribing
            var zombieQuery = _context.InterviewAnswers
                .Where(a => a.Status == AudioProcessingStatus.Transcribing);

            if (!isStartup)
            {
                var thresholdTime = DateTime.UtcNow.Subtract(ZombieTimeout);
                zombieQuery = zombieQuery.Where(a => a.ProcessingStartedAt != null && a.ProcessingStartedAt < thresholdTime);
            }

            var zombieAnswers = await zombieQuery.ToListAsync(cancellationToken);

            foreach (var answer in zombieAnswers)
            {
                if (answer.RetryCount >= MaxZombieRetries)
                {
                    var errorMsg = $"Đã vượt quá số lần thử lại tối đa ({MaxZombieRetries}) do tác vụ bị gián đoạn hoặc server dừng đột ngột.";
                    answer.MarkFailed(errorMsg);
                    response.ZombieJobsFailed++;

                    _logger.LogWarning(
                        "Zombie job #{AnswerId} đã đạt số lần thử lại tối đa ({RetryCount}/{MaxRetries}) -> Đánh dấu Failed.",
                        answer.Id, answer.RetryCount, MaxZombieRetries);
                }
                else
                {
                    answer.RequeueForRetry();
                    await _queue.EnqueueAsync(new AudioTranscriptionJob
                    {
                        InterviewAnswerId = answer.Id,
                        EnqueuedAt = DateTime.UtcNow
                    }, cancellationToken);

                    response.ZombieJobsRecovered++;

                    _logger.LogInformation(
                        "Zombie job #{AnswerId} (lần retry {RetryCount}/{MaxRetries}) đã được đưa về Pending và nạp lại vào hàng đợi.",
                        answer.Id, answer.RetryCount, MaxZombieRetries);
                }
            }

            // 2. Khi server khởi động lại (isStartup = true), nạp lại toàn bộ job đang ở trạng thái Pending
            // vì in-memory Channel queue đã bị xóa sạch khi process cũ dừng
            if (isStartup)
            {
                var recoveredZombieIds = zombieAnswers.Select(z => z.Id).ToHashSet();

                var pendingAnswers = await _context.InterviewAnswers
                    .Where(a => a.Status == AudioProcessingStatus.Pending)
                    .ToListAsync(cancellationToken);

                foreach (var pending in pendingAnswers)
                {
                    // Tránh nạp trùng các zombie vừa mới chuyển sang Pending ở bước 1
                    if (recoveredZombieIds.Contains(pending.Id))
                    {
                        continue;
                    }

                    await _queue.EnqueueAsync(new AudioTranscriptionJob
                    {
                        InterviewAnswerId = pending.Id,
                        EnqueuedAt = pending.CreatedAt
                    }, cancellationToken);

                    response.PendingJobsRequeued++;

                    _logger.LogInformation(
                        "Pending job #{AnswerId} chưa xử lý trước khi restart đã được nạp lại vào hàng đợi.",
                        pending.Id);
                }

                // Phase 4: Nạp lại toàn bộ backlog các answer đã bóc băng xong đang chờ chấm ở trạng thái Scoring
                var scoringAnswers = await _context.InterviewAnswers
                    .Where(a => a.Status == AudioProcessingStatus.Scoring)
                    .ToListAsync(cancellationToken);

                foreach (var scoring in scoringAnswers)
                {
                    await _queue.EnqueueAsync(new AudioTranscriptionJob
                    {
                        InterviewAnswerId = scoring.Id,
                        EnqueuedAt = scoring.UpdatedAt
                    }, cancellationToken);

                    response.PendingJobsRequeued++;

                    _logger.LogInformation(
                        "Scoring backlog job #{AnswerId} đã được nạp lại vào hàng đợi để chấm điểm AI.",
                        scoring.Id);
                }
            }

            if (response.TotalProcessed > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation(
                "Phục hồi tác vụ bóc băng hoàn tất: {Recovered} zombie requeued, {Failed} zombie failed, {Pending} pending requeued.",
                response.ZombieJobsRecovered, response.ZombieJobsFailed, response.PendingJobsRequeued);

            return response;
        }
    }
}

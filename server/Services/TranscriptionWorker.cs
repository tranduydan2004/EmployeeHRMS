using System;
using System.Threading;
using System.Threading.Tasks;
using EmployeeHRMS.Api.Data;
using EmployeeHRMS.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Background Service (Consumer) lắng nghe Channel<AudioTranscriptionJob> và xử lý bóc băng.
    /// 
    /// Quy trình cho mỗi job:
    /// 1. Tạo DI scope riêng (vì BackgroundService là Singleton, AppDbContext là Scoped).
    /// 2. Đọc InterviewAnswer từ DB, gọi domain method StartTranscribing() → trạng thái Transcribing.
    /// 3. Lấy audio stream từ IAudioStorageService.
    /// 4. Gọi ISpeechToTextService.TranscribeAsync() → nhận transcript text + verbose segments.
    /// 5. Gọi domain method CompleteTranscription() → trạng thái Scoring (Phase 4 sẽ xử lý tiếp).
    /// 6. Nếu lỗi: gọi MarkFailed() → trạng thái Failed, ghi log chi tiết.
    /// 
    /// Lưu ý graceful shutdown:
    /// - Khi app shutdown (stoppingToken fired), worker hoàn tất job đang xử lý hiện tại rồi mới dừng.
    /// - Job chưa được lấy từ Channel vẫn nằm trong queue nhưng sẽ MẤT nếu app restart
    ///   (đây là giới hạn cố hữu của in-memory queue — Bước 5 sẽ bổ sung startup recovery).
    /// </summary>
    public class TranscriptionWorker : BackgroundService
    {
        private readonly IAudioTranscriptionQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TranscriptionWorker> _logger;

        public TranscriptionWorker(
            IAudioTranscriptionQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<TranscriptionWorker> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TranscriptionWorker đã khởi động. Đang kích hoạt Startup Recovery...");

            // 1. Kích hoạt Startup Recovery không đồng bộ
            // Chạy trong Task riêng để không chặn Channel Reader bắt đầu tiêu thụ dữ liệu (tránh deadlock khi queue đầy)
            _ = Task.Run(async () =>
            {
                await RunStartupRecoveryAsync(stoppingToken);
            }, stoppingToken);

            // 2. Kích hoạt Periodic Zombie Job Check (mỗi 10 phút quét các job bị treo quá 15 phút)
            _ = Task.Run(async () =>
            {
                await RunPeriodicZombieCheckAsync(stoppingToken);
            }, stoppingToken);

            // 3. Lắng nghe và tiêu thụ Channel<AudioTranscriptionJob>
            try
            {
                // ReadAllAsync sẽ chờ bất đồng bộ khi Channel rỗng (không block thread)
                // và tự dừng khi stoppingToken fired hoặc Channel writer completed.
                await foreach (var job in _queue.Reader.ReadAllAsync(stoppingToken))
                {
                    await ProcessJobAsync(job, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("TranscriptionWorker đang dừng theo yêu cầu graceful shutdown.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TranscriptionWorker gặp lỗi nghiêm trọng không xử lý được.");
            }

            _logger.LogInformation("TranscriptionWorker đã dừng hoàn toàn.");
        }

        /// <summary>
        /// Quét và nạp lại tất cả các tác vụ Transcribing (zombie) và Pending khi server khởi động lại.
        /// </summary>
        private async Task RunStartupRecoveryAsync(CancellationToken stoppingToken)
        {
            try
            {
                // Chờ ngắn (500ms) để Channel Reader vòng lặp đã sẵn sàng tiếp nhận job
                await Task.Delay(500, stoppingToken);

                using var scope = _scopeFactory.CreateScope();
                var recoveryService = scope.ServiceProvider.GetRequiredService<ITranscriptionRecoveryService>();
                var result = await recoveryService.RecoverZombieJobsAsync(isStartup: true, stoppingToken);

                if (result.TotalProcessed > 0)
                {
                    _logger.LogInformation(
                        "Startup Recovery hoàn tất: {Recovered} zombie requeued, {Failed} zombie failed, {Pending} pending requeued.",
                        result.ZombieJobsRecovered, result.ZombieJobsFailed, result.PendingJobsRequeued);
                }
                else
                {
                    _logger.LogInformation("Startup Recovery: Không có tác vụ zombie hoặc pending nào cần phục hồi.");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình Startup Recovery.");
            }
        }

        /// <summary>
        /// Quét định kỳ mỗi 10 phút để phát hiện các job bị treo quá 15 phút trong runtime.
        /// </summary>
        private async Task RunPeriodicZombieCheckAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(10));
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var recoveryService = scope.ServiceProvider.GetRequiredService<ITranscriptionRecoveryService>();
                        var result = await recoveryService.RecoverZombieJobsAsync(isStartup: false, stoppingToken);

                        if (result.TotalProcessed > 0)
                        {
                            _logger.LogInformation(
                                "Periodic Zombie Check: {Recovered} zombie requeued, {Failed} zombie failed.",
                                result.ZombieJobsRecovered, result.ZombieJobsFailed);
                        }
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi xảy ra trong quá trình Periodic Zombie Check.");
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown
            }
        }

        /// <summary>
        /// Xử lý một job bóc băng: đọc DB → STT → cập nhật kết quả.
        /// Mỗi job được xử lý trong DI scope riêng biệt.
        /// Nếu job lỗi, nó KHÔNG làm sập worker — worker tiếp tục xử lý job tiếp theo.
        /// </summary>
        private async Task ProcessJobAsync(AudioTranscriptionJob job, CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Processing transcription job: AnswerId={AnswerId}, EnqueuedAt={EnqueuedAt}",
                job.InterviewAnswerId, job.EnqueuedAt);

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var audioStorage = scope.ServiceProvider.GetRequiredService<IAudioStorageService>();
            var sttService = scope.ServiceProvider.GetRequiredService<ISpeechToTextService>();

            try
            {
                // 1. Đọc InterviewAnswer từ DB
                var answer = await context.InterviewAnswers
                    .FirstOrDefaultAsync(a => a.Id == job.InterviewAnswerId, stoppingToken);

                if (answer == null)
                {
                    _logger.LogWarning(
                        "InterviewAnswer #{AnswerId} not found in DB. Skipping job (có thể đã bị xóa).",
                        job.InterviewAnswerId);
                    return;
                }

                // 2. Kiểm tra trạng thái hợp lệ (chỉ xử lý Pending hoặc Failed đang retry)
                if (answer.Status != AudioProcessingStatus.Pending && answer.Status != AudioProcessingStatus.Failed)
                {
                    _logger.LogWarning(
                        "InterviewAnswer #{AnswerId} has status '{Status}', expected Pending or Failed. Skipping duplicate job.",
                        job.InterviewAnswerId, answer.Status);
                    return;
                }

                // 3. Chuyển trạng thái sang Transcribing (domain method)
                answer.StartTranscribing();
                await context.SaveChangesAsync(stoppingToken);

                _logger.LogInformation(
                    "InterviewAnswer #{AnswerId} → Transcribing. AudioStorageKey={StorageKey}",
                    answer.Id, answer.AudioStorageKey);

                // 4. Lấy audio stream từ storage
                await using var audioStream = await audioStorage.GetAudioStreamAsync(
                    answer.AudioStorageKey, stoppingToken);

                // 5. Xác định tên file cho Whisper API (cần extension để nhận diện format)
                var fileName = ExtractFileName(answer.AudioStorageKey);

                // 6. Gọi Whisper STT (có retry nội bộ trong WhisperTranscriptionService)
                var result = await sttService.TranscribeAsync(audioStream, fileName, stoppingToken);

                // 7. Cập nhật kết quả vào entity (domain method chuyển sang Scoring)
                answer.CompleteTranscription(result.Text, result.SegmentsJson);
                await context.SaveChangesAsync(stoppingToken);

                _logger.LogInformation(
                    "Transcription completed for InterviewAnswer #{AnswerId}: {TextLength} chars, language={Language}. Status → Scoring.",
                    answer.Id, result.Text.Length, result.DetectedLanguage);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown đang diễn ra — job đang xử lý dở sẽ bị kẹt ở Transcribing.
                // Bước 5 (Startup Recovery) sẽ phát hiện và requeue zombie jobs khi app restart.
                _logger.LogWarning(
                    "Transcription job for AnswerId={AnswerId} interrupted by shutdown. Job remains in Transcribing state.",
                    job.InterviewAnswerId);
                throw; // Rethrow để thoát ExecuteAsync loop
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Transcription failed for InterviewAnswer #{AnswerId}: {Error}",
                    job.InterviewAnswerId, ex.Message);

                // Đánh dấu Failed trong DB
                await MarkJobFailedAsync(context, job.InterviewAnswerId, ex.Message, stoppingToken);
            }
        }

        /// <summary>
        /// Đánh dấu InterviewAnswer là Failed trong DB khi xử lý lỗi.
        /// Tách riêng try-catch để đảm bảo lỗi DB save không che giấu lỗi gốc.
        /// </summary>
        private async Task MarkJobFailedAsync(
            AppDbContext context,
            int answerId,
            string errorMessage,
            CancellationToken stoppingToken)
        {
            try
            {
                var answer = await context.InterviewAnswers
                    .FirstOrDefaultAsync(a => a.Id == answerId, CancellationToken.None);

                if (answer != null)
                {
                    // Truncate error message nếu quá dài
                    var truncatedError = errorMessage.Length > 1000
                        ? errorMessage[..1000]
                        : errorMessage;

                    answer.MarkFailed(truncatedError);
                    await context.SaveChangesAsync(CancellationToken.None);

                    _logger.LogInformation(
                        "InterviewAnswer #{AnswerId} → Failed. Error: {Error}",
                        answerId, truncatedError);
                }
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx,
                    "Failed to update InterviewAnswer #{AnswerId} status to Failed in DB. " +
                    "Job may remain in Transcribing state (zombie) until startup recovery.",
                    answerId);
            }
        }

        /// <summary>
        /// Trích xuất tên file từ storage key (Whisper API cần extension để nhận diện format).
        /// Ví dụ: "audio/interviews/1/20_abc123.webm" → "20_abc123.webm"
        /// </summary>
        private static string ExtractFileName(string storageKey)
        {
            if (string.IsNullOrWhiteSpace(storageKey))
                return "audio.webm";

            var lastSlash = storageKey.LastIndexOf('/');
            return lastSlash >= 0 && lastSlash < storageKey.Length - 1
                ? storageKey[(lastSlash + 1)..]
                : storageKey;
        }
    }
}

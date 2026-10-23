using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmployeeHRMS.Api.Configuration;
using EmployeeHRMS.Api.Data;
using EmployeeHRMS.Api.Models;
using EmployeeHRMS.Api.Services.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Background Service (Consumer) lắng nghe Channel<AudioTranscriptionJob> và xử lý toàn trình pipeline:
    /// 1. STT Phase: Whisper STT + tính chỉ số nhịp nói (SpeechMetricsCalculator) → Chuyển sang Scoring.
    /// 2. Scoring Phase: Chấm điểm rubric qua OpenAI (OpenAiScoringService) hoặc phát hiện NoResponse/NotScorable
    ///    → Tạo QuestionEvaluation (Audit Trail) → Chuyển sang Completed.
    /// 3. Aggregation Phase: Kiểm tra toàn bộ câu hỏi của Interview đã xong chưa → Gọi ScoreAggregator (Deterministic Rule Engine)
    ///    → Cập nhật điểm tổng thể & Đề xuất (Pass/Consider/Reject) → Gửi notification (D13).
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
        /// Quét và nạp lại tất cả các tác vụ Transcribing (zombie), Pending, và Scoring backlog khi server khởi động lại.
        /// </summary>
        private async Task RunStartupRecoveryAsync(CancellationToken stoppingToken)
        {
            try
            {
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
        /// Xử lý toàn trình một job: đọc DB → (Whisper STT nếu cần) → Rubric Scoring → Aggregation.
        /// Mỗi job được xử lý trong DI scope riêng biệt.
        /// </summary>
        private async Task ProcessJobAsync(AudioTranscriptionJob job, CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Processing audio interview job: AnswerId={AnswerId}, EnqueuedAt={EnqueuedAt}",
                job.InterviewAnswerId, job.EnqueuedAt);

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var audioStorage = scope.ServiceProvider.GetRequiredService<IAudioStorageService>();
            var sttService = scope.ServiceProvider.GetRequiredService<ISpeechToTextService>();
            var scoringService = scope.ServiceProvider.GetService<IOpenAiScoringService>();
            var scoringOptions = scope.ServiceProvider.GetService<IOptions<ScoringOptions>>()?.Value ?? new ScoringOptions();
            var speechMetricsOptions = scope.ServiceProvider.GetService<IOptions<SpeechMetricsOptions>>()?.Value ?? new SpeechMetricsOptions();

            AudioProcessingStage currentStage = AudioProcessingStage.Transcription;

            try
            {
                // 1. Đọc InterviewAnswer từ DB kèm Question và Evaluations
                var answer = await context.InterviewAnswers
                    .Include(a => a.InterviewQuestion)
                    .Include(a => a.Evaluations)
                    .FirstOrDefaultAsync(a => a.Id == job.InterviewAnswerId, stoppingToken);

                if (answer == null)
                {
                    _logger.LogWarning(
                        "InterviewAnswer #{AnswerId} not found in DB. Skipping job (có thể đã bị xóa).",
                        job.InterviewAnswerId);
                    return;
                }

                // 2. Kiểm tra trạng thái hợp lệ (Pending, Failed đang retry, hoặc Scoring từ backlog/re-evaluate)
                if (answer.Status != AudioProcessingStatus.Pending &&
                    answer.Status != AudioProcessingStatus.Failed &&
                    answer.Status != AudioProcessingStatus.Scoring)
                {
                    _logger.LogWarning(
                        "InterviewAnswer #{AnswerId} has status '{Status}', expected Pending, Failed, or Scoring. Skipping duplicate job.",
                        job.InterviewAnswerId, answer.Status);
                    return;
                }

                // 3. Giai đoạn 1: Whisper STT (Bỏ qua nếu answer đã ở trạng thái Scoring)
                if (answer.Status != AudioProcessingStatus.Scoring)
                {
                    currentStage = AudioProcessingStage.Transcription;
                    answer.StartTranscribing();
                    await context.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation(
                        "InterviewAnswer #{AnswerId} → Transcribing. AudioStorageKey={StorageKey}",
                        answer.Id, answer.AudioStorageKey);

                    await using var audioStream = await audioStorage.GetAudioStreamAsync(
                        answer.AudioStorageKey, stoppingToken);

                    var fileName = ExtractFileName(answer.AudioStorageKey);
                    var sttResult = await sttService.TranscribeAsync(audioStream, fileName, stoppingToken);

                    // Phân tích word timestamps và tính SpeechMetrics
                    var (words, _) = SpeechMetricsCalculator.ParseSegmentsJson(sttResult.SegmentsJson);
                    var speechMetrics = SpeechMetricsCalculator.Calculate(words, sttResult.DurationSeconds, speechMetricsOptions);

                    // Chuyển sang trạng thái Scoring
                    answer.CompleteTranscription(
                        sttResult.Text,
                        sttResult.SegmentsJson,
                        sttResult.DetectedLanguage,
                        sttResult.DurationSeconds,
                        speechMetrics);

                    await context.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation(
                        "Transcription completed for InterviewAnswer #{AnswerId}: {TextLength} chars, language={Language}, WPM={Wpm}. Status → Scoring.",
                        answer.Id, sttResult.Text.Length, sttResult.DetectedLanguage, speechMetrics.Wpm);
                }

                // 4. Giai đoạn 2: Rubric Scoring (AI Heuristics & Rubric Grading)
                currentStage = AudioProcessingStage.Scoring;
                if (scoringService != null)
                {
                    await ProcessScoringStageAsync(context, scoringService, answer, scoringOptions, speechMetricsOptions, stoppingToken);

                    // 5. Giai đoạn 3: Kiểm tra hoàn tất và tổng hợp điểm toàn buổi phỏng vấn (Strict Completion)
                    if (answer.InterviewQuestion != null)
                    {
                        await CheckAndAggregateInterviewAsync(context, scope, answer.InterviewQuestion.InterviewId, scoringOptions, stoppingToken);
                    }
                }
                else
                {
                    _logger.LogWarning("IOpenAiScoringService is not registered. InterviewAnswer #{AnswerId} remains in Scoring state.", answer.Id);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Job for AnswerId={AnswerId} interrupted by shutdown. Job remains in current processing state.",
                    job.InterviewAnswerId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Job processing failed for InterviewAnswer #{AnswerId} at stage '{Stage}': {Error}",
                    job.InterviewAnswerId, currentStage, ex.Message);

                await MarkJobFailedAsync(context, job.InterviewAnswerId, currentStage, ex.Message, stoppingToken);
            }
        }

        /// <summary>
        /// Xử lý chấm điểm câu trả lời: phân loại Silence/Hallucination, kiểm tra rubric, gọi LLM, lưu Audit Trail.
        /// </summary>
        private async Task ProcessScoringStageAsync(
            AppDbContext context,
            IOpenAiScoringService scoringService,
            InterviewAnswer answer,
            ScoringOptions scoringOptions,
            SpeechMetricsOptions speechMetricsOptions,
            CancellationToken stoppingToken)
        {
            var effectiveTranscript = answer.GetEffectiveTranscript();
            var (_, segments) = SpeechMetricsCalculator.ParseSegmentsJson(answer.TranscriptSegments);

            bool isHallucination = SpeechMetricsCalculator.IsKnownHallucination(effectiveTranscript, speechMetricsOptions);
            bool isSilence = string.IsNullOrWhiteSpace(effectiveTranscript) ||
                             SpeechMetricsCalculator.IsSilentSegments(segments, speechMetricsOptions);

            int nextVersion = (answer.Evaluations.Any() ? answer.Evaluations.Max(e => e.VersionNumber) : 0) + 1;

            // 1. Trường hợp Audio im lặng hoặc chỉ gồm cụm từ ảo giác (NoResponse)
            if (isHallucination || isSilence)
            {
                string reason = isHallucination ? "KnownHallucination" : "Silence";

                foreach (var prev in answer.Evaluations.Where(e => e.IsActive))
                {
                    prev.Deactivate();
                }

                var noResponseEval = QuestionEvaluation.CreateNoResponse(
                    answer.Id,
                    nextVersion,
                    effectiveTranscript ?? string.Empty,
                    reason,
                    scoringOptions.ModelName,
                    scoringOptions.PromptVersion);

                context.QuestionEvaluations.Add(noResponseEval);

                if (answer.InterviewQuestion != null)
                {
                    answer.InterviewQuestion.AiQuestionScore = 0.0m;
                    answer.InterviewQuestion.AiFeedback = $"Không phát hiện câu trả lời hợp lệ ({reason}).";
                }

                answer.CompleteScoring();
                await context.SaveChangesAsync(stoppingToken);

                _logger.LogInformation(
                    "InterviewAnswer #{AnswerId} evaluated as NoResponse ({Reason}). Status → Completed.",
                    answer.Id, reason);
                return;
            }

            // 2. Trường hợp câu hỏi thiếu barem rubric -> NotScorable (D3, D10)
            var question = answer.InterviewQuestion;
            if (question?.ScoringRubric == null ||
                (string.IsNullOrWhiteSpace(question.ScoringRubric.Poor) &&
                 string.IsNullOrWhiteSpace(question.ScoringRubric.Acceptable) &&
                 string.IsNullOrWhiteSpace(question.ScoringRubric.Good) &&
                 string.IsNullOrWhiteSpace(question.ScoringRubric.Excellent)))
            {
                string reason = "MissingRubric";

                foreach (var prev in answer.Evaluations.Where(e => e.IsActive))
                {
                    prev.Deactivate();
                }

                var notScorableEval = QuestionEvaluation.CreateNotScorable(
                    answer.Id,
                    nextVersion,
                    effectiveTranscript!,
                    reason,
                    "Câu hỏi không có barem chấm điểm ScoringRubric.",
                    scoringOptions.ModelName,
                    scoringOptions.PromptVersion);

                context.QuestionEvaluations.Add(notScorableEval);

                if (question != null)
                {
                    question.AiQuestionScore = null;
                    question.AiFeedback = "Câu hỏi chưa có barem chấm điểm (ScoringRubric).";
                }

                answer.CompleteScoring();
                await context.SaveChangesAsync(stoppingToken);

                _logger.LogInformation(
                    "InterviewAnswer #{AnswerId} evaluated as NotScorable ({Reason}). Status → Completed.",
                    answer.Id, reason);
                return;
            }

            // 3. Trường hợp bình thường có rubric và transcript -> Gọi OpenAI Scoring
            var scoringRequest = new QuestionScoringRequest
            {
                QuestionId = question.Id,
                QuestionText = question.Question,
                Category = question.Category,
                ScoringRubric = question.ScoringRubric,
                TranscriptSnapshot = effectiveTranscript!
            };

            var scoringResult = await scoringService.ScoreAnswerAsync(scoringRequest, stoppingToken);
            if (scoringResult == null)
            {
                throw new InvalidOperationException($"IOpenAiScoringService returned null result for Question #{question.Id}.");
            }

            foreach (var prev in answer.Evaluations.Where(e => e.IsActive))
            {
                prev.Deactivate();
            }

            var succeededEval = QuestionEvaluation.CreateSucceeded(
                answer.Id,
                nextVersion,
                effectiveTranscript!,
                scoringResult.Level,
                scoringResult.Score,
                scoringResult.Analysis,
                scoringResult.EvidenceQuotes,
                scoringResult.Strengths,
                scoringResult.Concerns,
                scoringResult.ModelName,
                scoringResult.PromptVersion);

            context.QuestionEvaluations.Add(succeededEval);

            question.AiQuestionScore = scoringResult.Score;
            question.AiFeedback = scoringResult.Analysis;

            answer.CompleteScoring();
            await context.SaveChangesAsync(stoppingToken);

            _logger.LogInformation(
                "InterviewAnswer #{AnswerId} scored successfully: Level={Level}, Score={Score:F1}. Status → Completed.",
                answer.Id, scoringResult.Level, scoringResult.Score);
        }

        /// <summary>
        /// Kiểm tra tính toàn vẹn (Strict Completion) và tổng hợp điểm cho toàn buổi phỏng vấn.
        /// </summary>
        private async Task CheckAndAggregateInterviewAsync(
            AppDbContext context,
            IServiceScope scope,
            int interviewId,
            ScoringOptions scoringOptions,
            CancellationToken stoppingToken)
        {
            var interview = await context.Interviews
                .Include(i => i.Questions)
                    .ThenInclude(q => q.Answer)
                        .ThenInclude(a => a!.Evaluations)
                .FirstOrDefaultAsync(i => i.Id == interviewId, stoppingToken);

            if (interview == null || interview.Questions.Count == 0)
            {
                return;
            }

            // Kiểm tra xem tất cả các câu hỏi đã có Answer và Answer đều ở trạng thái Completed chưa
            bool allCompleted = interview.Questions.All(q =>
                q.Answer != null && q.Answer.Status == AudioProcessingStatus.Completed);

            if (!allCompleted)
            {
                _logger.LogInformation(
                    "Interview #{InterviewId}: Chưa hoàn tất toàn bộ câu hỏi ({CompletedCount}/{TotalCount}). Bỏ qua aggregation.",
                    interviewId,
                    interview.Questions.Count(q => q.Answer != null && q.Answer.Status == AudioProcessingStatus.Completed),
                    interview.Questions.Count);
                return;
            }

            var evalRecords = new List<QuestionEvaluationRecord>();
            foreach (var q in interview.Questions)
            {
                var activeEval = q.Answer!.Evaluations.FirstOrDefault(e => e.IsActive);
                if (activeEval != null)
                {
                    evalRecords.Add(new QuestionEvaluationRecord
                    {
                        QuestionId = q.Id,
                        OrderIndex = q.OrderIndex,
                        Category = q.Category,
                        Outcome = activeEval.Outcome,
                        Score = activeEval.Score,
                        Level = activeEval.Level,
                        Reason = activeEval.Reason
                    });
                }
            }

            var aggResult = ScoreAggregator.Aggregate(evalRecords, interview.Questions.Count, scoringOptions);

            if (aggResult.IsFullyEvaluated)
            {
                interview.SetAggregationResult(aggResult.OverallScore, aggResult.Recommendation, aggResult.Summary);

                // Chuyển sang Completed nếu đang InProgress hoặc Expired (D4)
                if (interview.Status == InterviewStatus.InProgress || interview.Status == InterviewStatus.Expired)
                {
                    interview.Complete(aggResult.Summary, aggResult.OverallScore, aggResult.Recommendation);
                }

                await context.SaveChangesAsync(stoppingToken);

                _logger.LogInformation(
                    "Interview #{InterviewId} Aggregation completed: OverallScore={OverallScore}, Recommendation={Recommendation}",
                    interviewId, aggResult.OverallScore, aggResult.Recommendation);

                // Gửi notification real-time (D13)
                await SendInterviewCompletedNotificationAsync(scope, interview);
            }
        }

        /// <summary>
        /// Gửi thông báo real-time khi buổi phỏng vấn được AI chấm điểm hoàn tất (D13).
        /// </summary>
        private async Task SendInterviewCompletedNotificationAsync(IServiceScope scope, Interview interview)
        {
            try
            {
                var notificationService = scope.ServiceProvider.GetService<INotificationService>();
                if (notificationService == null) return;

                var payload = new Dictionary<string, string>
                {
                    ["InterviewId"] = interview.Id.ToString(),
                    ["Recommendation"] = interview.Recommendation?.ToString() ?? "N/A",
                    ["OverallScore"] = interview.AiOverallScore?.ToString("F1") ?? "N/A"
                };

                await notificationService.SendToRolesAsync(
                    new[] { UserRole.Admin, UserRole.HR },
                    "Phỏng vấn hoàn tất",
                    "INTERVIEW_COMPLETED",
                    payload,
                    NotificationType.Success,
                    relatedEntity: "Interview",
                    relatedEntityId: interview.Id,
                    fallbackMessage: $"Buổi phỏng vấn #{interview.Id} đã được AI chấm điểm: Đề xuất {interview.Recommendation}.");

                if (interview.InterviewerId.HasValue)
                {
                    await notificationService.SendToUserAsync(
                        interview.InterviewerId.Value,
                        "Phỏng vấn hoàn tất",
                        "INTERVIEW_COMPLETED",
                        payload,
                        NotificationType.Success,
                        relatedEntity: "Interview",
                        relatedEntityId: interview.Id,
                        fallbackMessage: $"Buổi phỏng vấn #{interview.Id} bạn phụ trách đã được AI chấm điểm: Đề xuất {interview.Recommendation}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể gửi notification cho Interview #{InterviewId}.", interview.Id);
            }
        }

        /// <summary>
        /// Đánh dấu InterviewAnswer là Failed trong DB khi xử lý lỗi tại giai đoạn tương ứng.
        /// </summary>
        private async Task MarkJobFailedAsync(
            AppDbContext context,
            int answerId,
            AudioProcessingStage stage,
            string errorMessage,
            CancellationToken stoppingToken)
        {
            try
            {
                var answer = await context.InterviewAnswers
                    .Include(a => a.Evaluations)
                    .FirstOrDefaultAsync(a => a.Id == answerId, CancellationToken.None);

                if (answer != null)
                {
                    var truncatedError = errorMessage.Length > 1000
                        ? errorMessage[..1000]
                        : errorMessage;

                    // Kiểm tra theo D9: nếu đang là chấm lại và đã có đánh giá hợp lệ trước đó
                    bool hasPriorActiveEvaluation = answer.Evaluations.Any(e => e.IsActive && e.Outcome == EvaluationOutcome.Succeeded);

                    if (stage == AudioProcessingStage.Scoring && hasPriorActiveEvaluation)
                    {
                        answer.RevertToCompletedAfterFailedReevaluation(truncatedError);
                        int nextVersion = answer.Evaluations.Max(e => e.VersionNumber) + 1;
                        var failedEval = QuestionEvaluation.CreateFailed(
                            answer.Id,
                            nextVersion,
                            answer.GetEffectiveTranscript() ?? string.Empty,
                            truncatedError,
                            "unknown",
                            "v1.0",
                            isActive: false);
                        context.QuestionEvaluations.Add(failedEval);
                    }
                    else
                    {
                        answer.MarkFailed(stage, truncatedError);
                        if (stage == AudioProcessingStage.Scoring)
                        {
                            int nextVersion = (answer.Evaluations.Any() ? answer.Evaluations.Max(e => e.VersionNumber) : 0) + 1;
                            var failedEval = QuestionEvaluation.CreateFailed(
                                answer.Id,
                                nextVersion,
                                answer.GetEffectiveTranscript() ?? string.Empty,
                                truncatedError,
                                "unknown",
                                "v1.0",
                                isActive: true);
                            context.QuestionEvaluations.Add(failedEval);
                        }
                    }

                    await context.SaveChangesAsync(CancellationToken.None);

                    _logger.LogInformation(
                        "InterviewAnswer #{AnswerId} → Failed at stage '{Stage}'. Error: {Error}",
                        answerId, stage, truncatedError);
                }
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx,
                    "Failed to update InterviewAnswer #{AnswerId} status in DB.",
                    answerId);
            }
        }

        /// <summary>
        /// Trích xuất tên file từ storage key.
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

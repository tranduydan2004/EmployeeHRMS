using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmployeeHRMS.Api.Configuration;
using EmployeeHRMS.Api.Data;
using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.Exceptions;
using EmployeeHRMS.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Service xử lý tiếp nhận file ghi âm câu trả lời phỏng vấn của ứng viên (Producer).
    /// Quy trình gồm 6 bước chặt chẽ:
    /// 1. Xác thực Interview qua InviteToken bí mật (chống IDOR).
    /// 2. Kiểm tra câu hỏi thuộc Interview và chưa từng có câu trả lời (Uniqueness).
    /// 3. Xác thực file audio 4 lớp (Extension, MIME, Size, Magic Bytes).
    /// 4. Lưu stream audio vào hệ thống lưu trữ qua IAudioStorageService.
    /// 5. Tạo bản ghi InterviewAnswer với trạng thái Pending trong Database.
    /// 6. Đẩy job vào Channel<AudioTranscriptionJob> in-memory bounded queue cho background worker bóc băng.
    /// Có cơ chế bù trừ (Compensating action) tự động xóa file nếu DB/Queue gặp sự cố.
    /// </summary>
    public class AudioIngestionService : IAudioIngestionService
    {
        private readonly AppDbContext _context;
        private readonly IAudioStorageService _audioStorageService;
        private readonly IFileValidatorService _fileValidatorService;
        private readonly IAudioTranscriptionQueue _transcriptionQueue;
        private readonly ILogger<AudioIngestionService> _logger;
        private readonly AudioInterviewOptions _audioInterviewOptions;

        public AudioIngestionService(
            AppDbContext context,
            IAudioStorageService audioStorageService,
            IFileValidatorService fileValidatorService,
            IAudioTranscriptionQueue transcriptionQueue,
            ILogger<AudioIngestionService> logger,
            IOptions<AudioInterviewOptions>? audioInterviewOptions = null)
        {
            _context = context;
            _audioStorageService = audioStorageService;
            _fileValidatorService = fileValidatorService;
            _transcriptionQueue = transcriptionQueue;
            _logger = logger;
            _audioInterviewOptions = audioInterviewOptions?.Value ?? new AudioInterviewOptions();
        }

        public async Task<(CandidateAudioAnswerResponseDto Response, bool IsCreated)> ProcessAudioUploadAsync(
            Guid inviteToken,
            int questionId,
            AudioAnswerUploadDto dto,
            CancellationToken cancellationToken = default)
        {
            // 1. Validate file đầu vào trước để fail fast nếu file rỗng / null
            if (dto.AudioFile == null || dto.AudioFile.Length == 0)
            {
                throw new BusinessRuleException("Vui lòng tải lên file âm thanh hợp lệ.");
            }

            // 2. Validate sâu tính hợp lệ của file audio (Size, Extension, MIME, Magic Bytes)
            await _fileValidatorService.ValidateAudioFileAsync(dto.AudioFile);

            // 3. Lookup Interview bằng InviteToken
            var interview = await _context.Interviews
                .Include(i => i.Questions)
                .FirstOrDefaultAsync(i => i.InviteToken == inviteToken, cancellationToken);

            if (interview == null)
            {
                _logger.LogWarning("Audio ingestion rejected: Interview not found with InviteToken {InviteToken}", inviteToken);
                throw new NotFoundException("Không tìm thấy buổi phỏng vấn với liên kết đã cung cấp.");
            }

            // 4. Kiểm tra trạng thái buổi phỏng vấn & thời hạn
            if (interview.IsInviteExpired(DateTime.UtcNow) || interview.Status == InterviewStatus.Expired)
            {
                if (interview.Status != InterviewStatus.Expired)
                {
                    interview.MarkExpired();
                    await _context.SaveChangesAsync(cancellationToken);
                }
                throw new BusinessRuleException("Liên kết phỏng vấn này đã hết hạn.", System.Net.HttpStatusCode.Gone);
            }

            if (interview.Status == InterviewStatus.Cancelled)
            {
                throw new BusinessRuleException("Buổi phỏng vấn này đã bị hủy.");
            }

            if (interview.Status == InterviewStatus.Completed)
            {
                throw new ConflictException("Buổi phỏng vấn này đã hoàn tất, không thể nộp thêm câu trả lời.");
            }

            // 4b. Kiểm tra SessionNonce để chống xung đột nhiều thiết bị/tab cùng lúc
            if (interview.ActiveSessionNonce.HasValue && dto.SessionNonce.HasValue)
            {
                if (interview.ActiveSessionNonce.Value != dto.SessionNonce.Value)
                {
                    _logger.LogWarning("Audio ingestion rejected: Invalid SessionNonce {ClientNonce} for Interview #{InterviewId}",
                        dto.SessionNonce, interview.Id);
                    throw new ConflictException("Phiên làm bài này đã được mở trên một thiết bị/tab khác. Vui lòng làm mới trang.");
                }
            }

            // 5. Kiểm tra câu hỏi có thuộc interview này không
            var question = interview.Questions.FirstOrDefault(q => q.Id == questionId);
            if (question == null)
            {
                _logger.LogWarning("Audio ingestion rejected: Question #{QuestionId} does not belong to Interview #{InterviewId}",
                    questionId, interview.Id);
                throw new NotFoundException($"Câu hỏi #{questionId} không tồn tại trong buổi phỏng vấn này.");
            }

            // 5a. Kiểm tra thứ tự một chiều: toàn bộ câu hỏi trước question.OrderIndex phải đã có câu trả lời
            var answeredQuestionIds = await _context.InterviewAnswers
                .Where(a => a.InterviewId == interview.Id)
                .Select(a => a.InterviewQuestionId)
                .ToListAsync(cancellationToken);

            var previousUnanswered = interview.Questions
                .Where(q => q.OrderIndex < question.OrderIndex && !answeredQuestionIds.Contains(q.Id))
                .OrderBy(q => q.OrderIndex)
                .FirstOrDefault();

            if (previousUnanswered != null)
            {
                _logger.LogWarning("Audio ingestion rejected: Sequence violation. Question #{QuestionId} (Order {Order}) submitted before Question #{PrevId} (Order {PrevOrder})",
                    questionId, question.OrderIndex, previousUnanswered.Id, previousUnanswered.OrderIndex);
                throw new BusinessRuleException($"Vui lòng hoàn thành câu hỏi số {previousUnanswered.OrderIndex + 1} trước theo đúng thứ tự.");
            }

            // 5b. Validate thời lượng câu trả lời theo đúng Question.AnswerSeconds + buffer dung sai (mặc định 5s)
            var allowedAnswerSeconds = question.AnswerSeconds > 0 ? question.AnswerSeconds : _audioInterviewOptions.DefaultAnswerSeconds;
            var maxAllowedSeconds = allowedAnswerSeconds + _audioInterviewOptions.DurationBufferSeconds;

            if (dto.DurationSeconds > maxAllowedSeconds)
            {
                _logger.LogWarning(
                    "Audio ingestion rejected: Duration {Duration}s exceeds limit {Allowed}s (+{Buffer}s buffer) for Question #{QuestionId}",
                    dto.DurationSeconds, allowedAnswerSeconds, _audioInterviewOptions.DurationBufferSeconds, questionId);

                throw new BusinessRuleException(
                    $"Thời lượng câu trả lời ({dto.DurationSeconds} giây) vượt quá giới hạn cho phép của câu hỏi này ({allowedAnswerSeconds} giây + {_audioInterviewOptions.DurationBufferSeconds} giây dung sai).");
            }

            // 5c. Xác thực mốc ServedAt và tính độ trễ nộp bài
            question.ServedAt ??= DateTime.UtcNow.AddSeconds(-dto.DurationSeconds);
            var elapsedSinceServed = (DateTime.UtcNow - question.ServedAt.Value).TotalSeconds;
            var thinkSeconds = question.ThinkSeconds > 0 ? question.ThinkSeconds : 20;
            var totalAllowedTime = thinkSeconds + allowedAnswerSeconds + _audioInterviewOptions.DurationBufferSeconds;

            // 6. Kiểm tra xem câu hỏi này đã có câu trả lời chưa — Idempotent (A5): trả 200 OK kèm answer hiện tại
            var existingAnswer = await _context.InterviewAnswers
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.InterviewQuestionId == questionId, cancellationToken);

            if (existingAnswer != null)
            {
                _logger.LogInformation("Audio ingestion duplicate hit: Question #{QuestionId} already has an answer #{AnswerId}. Returning existing answer idempotently.",
                    questionId, existingAnswer.Id);
                return (MapToCandidateResponseDto(existingAnswer), false);
            }

            // 7. Lưu trữ audio vào IAudioStorageService
            var extension = Path.GetExtension(dto.AudioFile.FileName);
            var storageKey = _audioStorageService.GenerateStorageKey(interview.Id, questionId, extension);

            await using (var fileStream = dto.AudioFile.OpenReadStream())
            {
                await _audioStorageService.SaveAudioAsync(
                    fileStream,
                    storageKey,
                    dto.AudioFile.ContentType,
                    cancellationToken);
            }

            // 8. Whitelist & lọc Flags do client khai báo (A4) kết hợp phát hiện trễ hạn
            var safeFlags = dto.Flags?
                .Where(f => !string.IsNullOrWhiteSpace(f) && string.Equals(f.Trim(), "MicDisconnected", StringComparison.OrdinalIgnoreCase))
                .Select(f => "MicDisconnected")
                .Distinct()
                .Take(5)
                .ToList() ?? new List<string>();

            if (dto.IsOfflineRecovery)
            {
                safeFlags.Add("OfflineRecovery");
            }

            // Nếu nộp bài trễ hơn khung giờ suy nghĩ + trả lời + buffer
            if (elapsedSinceServed > totalAllowedTime)
            {
                var delayedSeconds = (int)Math.Round(elapsedSinceServed - totalAllowedTime);
                safeFlags.Add($"LateSubmission:{delayedSeconds}s");
                _logger.LogWarning(
                    "Audio answer for Question #{QuestionId} submitted late by {Delayed}s (Elapsed: {Elapsed:F1}s, Allowed: {Allowed}s). Flagged LateSubmission.",
                    questionId, delayedSeconds, elapsedSinceServed, totalAllowedTime);
            }

            // 9. Tạo bản ghi InterviewAnswer (Status = Pending) & Compensating transaction nếu DB / Queue fail
            InterviewAnswer answer;
            try
            {
                answer = new InterviewAnswer
                {
                    InterviewId = interview.Id,
                    InterviewQuestionId = questionId,
                    AudioStorageKey = storageKey,
                    AudioDurationSeconds = dto.DurationSeconds,
                    AudioMimeType = string.IsNullOrWhiteSpace(dto.AudioFile.ContentType)
                        ? "audio/webm"
                        : dto.AudioFile.ContentType,
                    AudioFileSize = dto.AudioFile.Length,
                    TabSwitchCount = dto.TabSwitchCount,
                    OutOfFocusDurationSeconds = dto.OutOfFocusDurationSeconds,
                    Flags = safeFlags,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.InterviewAnswers.Add(answer);
                await _context.SaveChangesAsync(cancellationToken);

                // 10. Enqueue job vào Channel queue (Producer)
                var job = new AudioTranscriptionJob
                {
                    InterviewAnswerId = answer.Id,
                    EnqueuedAt = DateTime.UtcNow
                };

                await _transcriptionQueue.EnqueueAsync(job, cancellationToken);

                _logger.LogInformation(
                    "Audio ingestion succeeded: Answer #{AnswerId} created for Interview #{InterviewId}, Question #{QuestionId}. Enqueued to channel.",
                    answer.Id, interview.Id, questionId);
            }
            catch (DbUpdateException dbEx)
            {
                // Bắt race condition giữa hai request retry đồng thời đụng unique constraint InterviewQuestionId
                _logger.LogWarning(dbEx, "DbUpdateException on Question #{QuestionId}. Checking for concurrent duplicate answer.", questionId);
                var concurrentAnswer = await _context.InterviewAnswers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.InterviewQuestionId == questionId, cancellationToken);

                if (concurrentAnswer != null)
                {
                    try
                    {
                        await _audioStorageService.DeleteAudioAsync(storageKey, CancellationToken.None);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogError(cleanupEx, "Failed to clean up duplicate audio file: {StorageKey}", storageKey);
                    }

                    return (MapToCandidateResponseDto(concurrentAnswer), false);
                }

                // Nếu là lỗi DB khác: xóa file audio đã lưu và rethrow
                await TryCompensatingDeleteAsync(storageKey);
                throw;
            }
            catch (Exception ex)
            {
                // Compensating action: Xóa file audio đã lưu nếu DB save hoặc Enqueue thất bại
                _logger.LogError(ex,
                    "Failed to save InterviewAnswer or enqueue job for Question #{QuestionId}. Compensating by deleting audio file {StorageKey}",
                    questionId, storageKey);

                await TryCompensatingDeleteAsync(storageKey);
                throw;
            }

            // 11. Trả về DTO kèm cờ isCreated = true (HTTP 201)
            return (MapToCandidateResponseDto(answer), true);
        }

        private async Task TryCompensatingDeleteAsync(string storageKey)
        {
            try
            {
                await _audioStorageService.DeleteAudioAsync(storageKey, CancellationToken.None);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogError(cleanupEx, "Failed to clean up audio file during compensating transaction: {StorageKey}", storageKey);
            }
        }

        private static CandidateAudioAnswerResponseDto MapToCandidateResponseDto(InterviewAnswer answer)
        {
            return new CandidateAudioAnswerResponseDto
            {
                QuestionId = answer.InterviewQuestionId,
                Received = true
            };
        }

        public async Task<CandidateAudioAnswerResponseDto> GetAnswerStatusAsync(
            Guid inviteToken,
            int questionId,
            CancellationToken cancellationToken = default)
        {
            var interview = await _context.Interviews
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InviteToken == inviteToken, cancellationToken);

            if (interview == null)
            {
                throw new NotFoundException("Không tìm thấy buổi phỏng vấn với liên kết đã cung cấp.");
            }

            var answer = await _context.InterviewAnswers
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.InterviewId == interview.Id && a.InterviewQuestionId == questionId, cancellationToken);

            if (answer == null)
            {
                throw new NotFoundException($"Chưa có câu trả lời cho câu hỏi #{questionId}.");
            }

            return MapToCandidateResponseDto(answer);
        }
    }
}

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmployeeHRMS.Api.Data;
using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.Exceptions;
using EmployeeHRMS.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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

        public AudioIngestionService(
            AppDbContext context,
            IAudioStorageService audioStorageService,
            IFileValidatorService fileValidatorService,
            IAudioTranscriptionQueue transcriptionQueue,
            ILogger<AudioIngestionService> logger)
        {
            _context = context;
            _audioStorageService = audioStorageService;
            _fileValidatorService = fileValidatorService;
            _transcriptionQueue = transcriptionQueue;
            _logger = logger;
        }

        public async Task<AudioAnswerResponseDto> ProcessAudioUploadAsync(
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

            // 4. Kiểm tra trạng thái buổi phỏng vấn
            if (interview.Status == InterviewStatus.Cancelled)
            {
                throw new BusinessRuleException("Buổi phỏng vấn này đã bị hủy.");
            }

            if (interview.Status == InterviewStatus.Completed)
            {
                throw new BusinessRuleException("Buổi phỏng vấn này đã hoàn tất, không thể nộp thêm câu trả lời.");
            }

            // 5. Kiểm tra câu hỏi có thuộc interview này không
            var question = interview.Questions.FirstOrDefault(q => q.Id == questionId);
            if (question == null)
            {
                _logger.LogWarning("Audio ingestion rejected: Question #{QuestionId} does not belong to Interview #{InterviewId}",
                    questionId, interview.Id);
                throw new NotFoundException($"Câu hỏi #{questionId} không tồn tại trong buổi phỏng vấn này.");
            }

            // 6. Kiểm tra xem câu hỏi này đã có câu trả lời chưa (mỗi câu chỉ được nộp 1 lần)
            var existingAnswer = await _context.InterviewAnswers
                .AnyAsync(a => a.InterviewQuestionId == questionId, cancellationToken);

            if (existingAnswer)
            {
                _logger.LogWarning("Audio ingestion rejected: Question #{QuestionId} already has an answer", questionId);
                throw new ConflictException("Câu hỏi này đã có câu trả lời được ghi nhận.");
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

            // 8. Tạo bản ghi InterviewAnswer (Status = Pending) & Compensating transaction nếu DB / Queue fail
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
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.InterviewAnswers.Add(answer);
                await _context.SaveChangesAsync(cancellationToken);

                // 9. Enqueue job vào Channel queue (Producer)
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
            catch (Exception ex)
            {
                // Compensating action: Xóa file audio đã lưu nếu DB save hoặc Enqueue thất bại
                _logger.LogError(ex,
                    "Failed to save InterviewAnswer or enqueue job for Question #{QuestionId}. Compensating by deleting audio file {StorageKey}",
                    questionId, storageKey);

                try
                {
                    await _audioStorageService.DeleteAudioAsync(storageKey, CancellationToken.None);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogError(cleanupEx, "Failed to clean up audio file during compensating transaction: {StorageKey}", storageKey);
                }

                throw;
            }

            // 10. Trả về DTO
            return new AudioAnswerResponseDto
            {
                Id = answer.Id,
                InterviewQuestionId = answer.InterviewQuestionId,
                AudioStorageKey = answer.AudioStorageKey,
                AudioDurationSeconds = answer.AudioDurationSeconds,
                AudioMimeType = answer.AudioMimeType,
                AudioFileSize = answer.AudioFileSize,
                Status = answer.Status.ToString(),
                Transcript = answer.Transcript,
                AiScore = answer.AiScore,
                ErrorMessage = answer.ErrorMessage,
                CreatedAt = answer.CreatedAt,
                CompletedAt = answer.CompletedAt
            };
        }

        public async Task<AudioAnswerResponseDto> GetAnswerStatusAsync(
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

            return new AudioAnswerResponseDto
            {
                Id = answer.Id,
                InterviewQuestionId = answer.InterviewQuestionId,
                AudioStorageKey = answer.AudioStorageKey,
                AudioDurationSeconds = answer.AudioDurationSeconds,
                AudioMimeType = answer.AudioMimeType,
                AudioFileSize = answer.AudioFileSize,
                Status = answer.Status.ToString(),
                Transcript = answer.Transcript,
                AiScore = answer.AiScore,
                ErrorMessage = answer.ErrorMessage,
                CreatedAt = answer.CreatedAt,
                CompletedAt = answer.CompletedAt
            };
        }
    }
}

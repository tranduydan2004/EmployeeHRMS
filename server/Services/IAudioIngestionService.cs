using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.Models;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Interface cho Audio Ingestion — nhận file audio từ ứng viên, validate, lưu trữ,
    /// tạo InterviewAnswer record và đẩy job vào Channel queue.
    /// </summary>
    public interface IAudioIngestionService
    {
        /// <summary>
        /// Xử lý upload audio cho một câu hỏi phỏng vấn:
        /// 1. Lookup Interview bằng InviteToken
        /// 2. Validate câu hỏi thuộc interview đó và chưa có câu trả lời
        /// 3. Validate file audio (magic bytes, size, extension)
        /// 4. Lưu file qua IAudioStorageService
        /// 5. Tạo InterviewAnswer record (Status = Pending)
        /// 6. Đẩy AudioTranscriptionJob vào Channel queue
        /// </summary>
        Task<(AudioAnswerResponseDto Response, bool IsCreated)> ProcessAudioUploadAsync(
            Guid inviteToken,
            int questionId,
            AudioAnswerUploadDto dto,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy thông tin và trạng thái xử lý của câu trả lời theo inviteToken và questionId.
        /// Dùng cho frontend polling trạng thái xử lý STT & AI scoring.
        /// </summary>
        Task<AudioAnswerResponseDto> GetAnswerStatusAsync(
            Guid inviteToken,
            int questionId,
            CancellationToken cancellationToken = default);
    }
}

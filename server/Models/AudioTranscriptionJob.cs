namespace EmployeeHRMS.Api.Models
{
    /// <summary>
    /// Đối tượng job được đẩy vào Channel<AudioTranscriptionJob> khi ứng viên nộp bài ghi âm.
    /// Consumer (TranscriptionWorker) nhận và xử lý STT qua Whisper API.
    /// Chỉ chứa ID tham chiếu — toàn bộ dữ liệu thực tế được đọc từ DB/Storage khi consumer xử lý.
    /// </summary>
    public class AudioTranscriptionJob
    {
        /// <summary>
        /// ID của InterviewAnswer cần xử lý bóc băng.
        /// </summary>
        public int InterviewAnswerId { get; init; }

        /// <summary>
        /// Thời điểm job được đẩy vào queue, dùng cho monitoring/logging.
        /// </summary>
        public DateTime EnqueuedAt { get; init; } = DateTime.UtcNow;
    }
}

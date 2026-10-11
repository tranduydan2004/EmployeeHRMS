namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Kết quả bóc băng từ Speech-to-Text service (Whisper API).
    /// Chứa transcript full-text và verbose JSON segments (word-level timestamps).
    /// </summary>
    public class TranscriptionResult
    {
        /// <summary>
        /// Full-text transcript (toàn bộ nội dung bóc băng).
        /// </summary>
        public string Text { get; init; } = string.Empty;

        /// <summary>
        /// JSON chứa verbose segments với word-level timestamps từ Whisper API.
        /// Dùng cho Phase 4: phân tích tốc độ nói (WPM), khoảng lặng, ngập ngừng.
        /// </summary>
        public string? SegmentsJson { get; init; }

        /// <summary>
        /// Ngôn ngữ phát hiện tự động bởi Whisper (ISO 639-1, vd: "vi", "en").
        /// </summary>
        public string? DetectedLanguage { get; init; }

        /// <summary>
        /// Thời lượng audio tính bằng giây (theo Whisper API response).
        /// </summary>
        public double? DurationSeconds { get; init; }
    }

    /// <summary>
    /// Interface trừu tượng hóa Speech-to-Text service.
    /// Cho phép thay thế giữa OpenAI Whisper, Azure Speech, hoặc mock cho unit tests.
    /// </summary>
    public interface ISpeechToTextService
    {
        /// <summary>
        /// Bóc băng file audio thành văn bản sử dụng STT API (Whisper).
        /// </summary>
        /// <param name="audioStream">Stream dữ liệu audio (phải ở vị trí 0 hoặc seekable)</param>
        /// <param name="fileName">Tên file audio kèm extension (cần thiết cho Whisper API nhận diện format)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>TranscriptionResult chứa transcript text và verbose segments JSON</returns>
        Task<TranscriptionResult> TranscribeAsync(
            Stream audioStream,
            string fileName,
            CancellationToken cancellationToken = default);
    }
}

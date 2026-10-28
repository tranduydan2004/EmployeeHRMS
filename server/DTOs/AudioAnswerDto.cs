using System.ComponentModel.DataAnnotations;

namespace EmployeeHRMS.Api.DTOs
{
    // === DTO nhận audio upload từ ứng viên ===
    public class AudioAnswerUploadDto
    {
        /// <summary>
        /// File ghi âm câu trả lời. MediaRecorder thường xuất WebM (Chrome/Firefox) hoặc MP4 (Safari).
        /// </summary>
        [Required(ErrorMessage = "Audio file is required.")]
        public IFormFile AudioFile { get; set; } = null!;

        /// <summary>
        /// Thời lượng ghi âm tính bằng giây (do client gửi kèm, tối đa 125 giây gồm trần 120s + 5s buffer).
        /// </summary>
        [Required(ErrorMessage = "Audio duration is required.")]
        [Range(1, 125, ErrorMessage = "Duration must be between 1 and 125 seconds.")]
        public int DurationSeconds { get; set; }

        // ============================================================
        // Anti-Cheat Telemetry (Phase 2 sẽ gửi, Phase 3 lưu)
        // ============================================================
        [Range(0, int.MaxValue)]
        public int TabSwitchCount { get; set; }

        [Range(0, double.MaxValue)]
        public double OutOfFocusDurationSeconds { get; set; }

        /// <summary>
        /// Danh sách flag bất thường do client phát hiện (ví dụ: "MicDisconnected").
        /// Server sẽ validate theo whitelist.
        /// </summary>
        public List<string>? Flags { get; set; }

        /// <summary>
        /// Nonce phiên hoạt động của ứng viên để chống gian lận mở nhiều tab/thiết bị đồng thời.
        /// </summary>
        public Guid? SessionNonce { get; set; }

        /// <summary>
        /// Cờ đánh dấu nộp từ kho lưu trữ offline cục bộ (IndexedDB) sau khi phục hồi đường truyền mạng.
        /// </summary>
        public bool IsOfflineRecovery { get; set; }
    }

    /// <summary>
    /// DTO an toàn trả về cho ứng viên sau khi upload audio hoặc polling trạng thái.
    /// Tuyệt đối KHÔNG chứa AiScore, AiFeedback, Transcript, ErrorMessage, Flags hay Rubric.
    /// </summary>
    public class CandidateAudioAnswerResponseDto
    {
        public int QuestionId { get; set; }
        public bool Received { get; set; }
    }

    // === DTO trả về sau khi upload audio thành công hoặc query status ===
    [Obsolete("Tuyệt đối không dùng cho endpoint ẩn danh của ứng viên để tránh lộ điểm hay dữ liệu nội bộ. Dùng CandidateAudioAnswerResponseDto.")]
    public class AudioAnswerResponseDto
    {
        public int Id { get; set; }
        public int InterviewQuestionId { get; set; }
        public string AudioStorageKey { get; set; } = string.Empty;
        public int AudioDurationSeconds { get; set; }
        public string AudioMimeType { get; set; } = string.Empty;
        public long AudioFileSize { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Transcript { get; set; }
        public decimal? AiScore { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    // === DTO trả về kết quả quét và phục hồi zombie/pending jobs ===
    public class TranscriptionRecoveryResponseDto
    {
        public int ZombieJobsRecovered { get; set; }
        public int ZombieJobsFailed { get; set; }
        public int PendingJobsRequeued { get; set; }
        public int TotalProcessed => ZombieJobsRecovered + ZombieJobsFailed + PendingJobsRequeued;
        public DateTime RecoveredAt { get; set; } = DateTime.UtcNow;
    }
}

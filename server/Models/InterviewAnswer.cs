using System;

namespace EmployeeHRMS.Api.Models
{
    public enum AudioProcessingStatus
    {
        Pending = 1,
        Transcribing = 2,
        Scoring = 3,
        Completed = 4,
        Failed = 5
    }

    /// <summary>
    /// Lưu trữ audio response, metadata chống gian lận, kết quả bóc băng (Whisper),
    /// và AI scoring cho từng câu hỏi phỏng vấn.
    /// Trạng thái xử lý (Status) được kiểm soát nghiêm ngặt qua Domain Methods.
    /// </summary>
    public class InterviewAnswer
    {
        public int Id { get; set; }

        public int InterviewId { get; set; }
        public Interview Interview { get; set; } = null!;

        public int InterviewQuestionId { get; set; }
        public InterviewQuestion InterviewQuestion { get; set; } = null!;

        // ============================================================
        // Audio Storage Metadata
        // ============================================================
        /// <summary>
        /// Key định danh vị trí lưu trữ file audio (hỗ trợ cả Local Disk lẫn Cloud Blob).
        /// </summary>
        public string AudioStorageKey { get; set; } = string.Empty;

        /// <summary>
        /// Thời lượng audio thực tế tính bằng giây.
        /// </summary>
        public int AudioDurationSeconds { get; set; }

        /// <summary>
        /// MIME type của audio (ví dụ audio/webm, audio/mp4).
        /// </summary>
        public string AudioMimeType { get; set; } = string.Empty;

        /// <summary>
        /// Dung lượng file audio tính bằng bytes.
        /// </summary>
        public long AudioFileSize { get; set; }

        // ============================================================
        // Anti-Cheat Telemetry (Phase 2 gửi, Phase 3 lưu)
        // ============================================================
        /// <summary>
        /// Số lần ứng viên rời khỏi hoặc chuyển tab trình duyệt trong khi ghi âm câu này.
        /// </summary>
        public int TabSwitchCount { get; set; }

        /// <summary>
        /// Tổng thời gian màn hình bị mất focus tính bằng giây.
        /// </summary>
        public double OutOfFocusDurationSeconds { get; set; }

        /// <summary>
        /// Flag do CLIENT tự khai báo (whitelist, ví dụ "MicDisconnected"). Chỉ để HR tham khảo,
        /// KHÔNG dùng để tự động tính điểm. Flag do server tự tính (Phase 4) lưu ở trường riêng.
        /// </summary>
        public List<string> Flags { get; set; } = new();

        // ============================================================
        // STT Output (Whisper API - Phase 3)
        // ============================================================
        /// <summary>
        /// Nội dung văn bản bóc băng từ giọng nói ứng viên.
        /// </summary>
        public string? Transcript { get; set; }

        /// <summary>
        /// Dữ liệu JSON chứa word-level timestamps và verbose segments từ Whisper API.
        /// </summary>
        public string? TranscriptSegments { get; set; }

        // ============================================================
        // AI Scoring & Feedback (Phase 4)
        // ============================================================
        public decimal? AiScore { get; set; }
        public string? AiFeedback { get; set; }

        // ============================================================
        // Processing State Machine (Channels & Background Worker)
        // ============================================================
        public AudioProcessingStatus Status { get; private set; } = AudioProcessingStatus.Pending;

        /// <summary>
        /// Timestamp bắt đầu xử lý bởi worker, dùng để phát hiện zombie jobs.
        /// </summary>
        public DateTime? ProcessingStartedAt { get; private set; }

        public DateTime? CompletedAt { get; private set; }

        public string? ErrorMessage { get; private set; }

        public int RetryCount { get; private set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ============================================================
        // Domain Methods — Quản lý chuyển trạng thái an toàn
        // ============================================================

        /// <summary>
        /// Worker nhận job và chuyển sang trạng thái bóc băng (Transcribing).
        /// </summary>
        public void StartTranscribing()
        {
            if (Status != AudioProcessingStatus.Pending && Status != AudioProcessingStatus.Failed)
            {
                throw new InvalidOperationException($"Cannot start transcribing from state '{Status}'.");
            }

            Status = AudioProcessingStatus.Transcribing;
            ProcessingStartedAt = DateTime.UtcNow;
            ErrorMessage = null;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Hoàn tất Whisper STT, lưu kết quả transcript và chuyển sang giai đoạn Scoring (Phase 4).
        /// </summary>
        public void CompleteTranscription(string transcript, string? transcriptSegments = null)
        {
            if (Status != AudioProcessingStatus.Transcribing)
            {
                throw new InvalidOperationException($"Cannot complete transcription from state '{Status}'. Expected '{AudioProcessingStatus.Transcribing}'.");
            }

            Transcript = transcript;
            TranscriptSegments = transcriptSegments;
            Status = AudioProcessingStatus.Scoring;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Hoàn tất chấm điểm AI (Phase 4) và đánh dấu câu trả lời hoàn tất.
        /// </summary>
        public void CompleteScoring(decimal score, string? feedback = null)
        {
            if (Status != AudioProcessingStatus.Scoring)
            {
                throw new InvalidOperationException($"Cannot complete scoring from state '{Status}'. Expected '{AudioProcessingStatus.Scoring}'.");
            }

            AiScore = score;
            AiFeedback = feedback;
            Status = AudioProcessingStatus.Completed;
            CompletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Ghi nhận lỗi xử lý tại bất kỳ bước nào trong pipeline.
        /// </summary>
        public void MarkFailed(string errorMessage)
        {
            Status = AudioProcessingStatus.Failed;
            ErrorMessage = errorMessage;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Đưa job bị lỗi hoặc bị timeout (zombie job) về lại hàng đợi Pending để retry.
        /// </summary>
        public void RequeueForRetry()
        {
            RetryCount++;
            Status = AudioProcessingStatus.Pending;
            ProcessingStartedAt = null;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}

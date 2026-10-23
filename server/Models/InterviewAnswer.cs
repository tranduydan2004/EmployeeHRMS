using System;
using System.Collections.Generic;
using EmployeeHRMS.Api.Models.ValueObjects;

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
    /// và lịch sử đánh giá rubric cho từng câu hỏi phỏng vấn.
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
        /// Thời lượng audio thực tế tính bằng giây do client đo được.
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
        // STT Output (Whisper API - Phase 3 & 4)
        // ============================================================
        /// <summary>
        /// Bản bóc băng gốc trực tiếp từ Whisper STT. Chỉ được ghi thông qua domain method CompleteTranscription (D6).
        /// </summary>
        public string? Transcript { get; private set; }

        /// <summary>
        /// Bản bóc băng sau khi HR/Interviewer chỉnh sửa. Dùng làm đầu vào ưu tiên khi chấm lại (Re-evaluate).
        /// </summary>
        public string? EditedTranscript { get; private set; }

        /// <summary>
        /// ID của người dùng HR/Interviewer chỉnh sửa transcript lần gần nhất.
        /// </summary>
        public int? LastEditedByUserId { get; private set; }

        /// <summary>
        /// Thời điểm chỉnh sửa transcript lần gần nhất.
        /// </summary>
        public DateTime? LastEditedAt { get; private set; }

        /// <summary>
        /// Ngôn ngữ do Whisper API nhận diện từ giọng nói (từ verbose_json).
        /// </summary>
        public string? DetectedLanguage { get; private set; }

        /// <summary>
        /// Thời lượng thực tế của đoạn audio được Whisper bóc băng tính bằng giây (từ verbose_json).
        /// Đáng tin cậy hơn thời lượng client gửi khi tính WPM (D2).
        /// </summary>
        public double? SttDurationSeconds { get; private set; }

        /// <summary>
        /// Dữ liệu JSON chứa word-level timestamps và verbose segments từ Whisper API.
        /// </summary>
        public string? TranscriptSegments { get; set; }

        /// <summary>
        /// Value Object lưu các chỉ số nhịp nói thuần túy (WPM, pause variation, v.v.) map sang cột jsonb.
        /// </summary>
        public SpeechMetrics? SpeechMetrics { get; private set; }

        // ============================================================
        // AI Scoring & Feedback (Phase 4: Audit Trail via QuestionEvaluation)
        // ============================================================
        [Obsolete("Đã chuyển sang bảng QuestionEvaluation theo kiến trúc audit trail. Không ghi mới vào cột này (D7).")]
        public decimal? AiScore { get; set; }

        [Obsolete("Đã chuyển sang bảng QuestionEvaluation theo kiến trúc audit trail. Không ghi mới vào cột này (D7).")]
        public string? AiFeedback { get; set; }

        /// <summary>
        /// Danh sách lịch sử các lần đánh giá rubric cho câu trả lời này (Audit Trail).
        /// </summary>
        public ICollection<QuestionEvaluation> Evaluations { get; set; } = new List<QuestionEvaluation>();

        // ============================================================
        // Processing State Machine (Channels & Background Worker)
        // ============================================================
        public AudioProcessingStatus Status { get; private set; } = AudioProcessingStatus.Pending;

        /// <summary>
        /// Giai đoạn xảy ra lỗi khi Status = Failed (Transcription hay Scoring) (D8).
        /// </summary>
        public AudioProcessingStage? FailedStage { get; private set; }

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
        /// Lấy văn bản bóc băng hiệu lực: ưu tiên EditedTranscript nếu có, ngược lại dùng Transcript gốc.
        /// </summary>
        public string? GetEffectiveTranscript() =>
            !string.IsNullOrWhiteSpace(EditedTranscript) ? EditedTranscript : Transcript;

        /// <summary>
        /// HR/Interviewer chỉnh sửa bản bóc băng.
        /// </summary>
        public void UpdateTranscript(string editedTranscript, int userId)
        {
            if (string.IsNullOrWhiteSpace(editedTranscript))
                throw new ArgumentException("Edited transcript cannot be empty.", nameof(editedTranscript));

            EditedTranscript = editedTranscript.Trim();
            LastEditedByUserId = userId;
            LastEditedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

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
            FailedStage = null;
            ErrorMessage = null;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Hoàn tất Whisper STT, lưu kết quả transcript và chuyển sang giai đoạn Scoring (Phase 4).
        /// </summary>
        public void CompleteTranscription(
            string transcript,
            string? transcriptSegments = null,
            string? detectedLanguage = null,
            double? sttDurationSeconds = null,
            SpeechMetrics? speechMetrics = null)
        {
            if (Status != AudioProcessingStatus.Transcribing)
            {
                throw new InvalidOperationException($"Cannot complete transcription from state '{Status}'. Expected '{AudioProcessingStatus.Transcribing}'.");
            }

            Transcript = transcript;
            TranscriptSegments = transcriptSegments;
            DetectedLanguage = detectedLanguage;
            SttDurationSeconds = sttDurationSeconds;
            SpeechMetrics = speechMetrics;
            Status = AudioProcessingStatus.Scoring;
            ProcessingStartedAt = DateTime.UtcNow;
            FailedStage = null;
            ErrorMessage = null;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Bắt đầu giai đoạn chấm điểm (Scoring) cho câu trả lời.
        /// </summary>
        public void StartScoring()
        {
            if (Status != AudioProcessingStatus.Completed &&
                Status != AudioProcessingStatus.Failed &&
                Status != AudioProcessingStatus.Scoring)
            {
                throw new InvalidOperationException($"Cannot start scoring from state '{Status}'.");
            }

            Status = AudioProcessingStatus.Scoring;
            ProcessingStartedAt = DateTime.UtcNow;
            FailedStage = null;
            ErrorMessage = null;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Hoàn tất pipeline chấm điểm AI cho câu trả lời (D10: Completed được set ngay cả khi NotScorable).
        /// </summary>
        public void CompleteScoring()
        {
            if (Status != AudioProcessingStatus.Scoring)
            {
                throw new InvalidOperationException($"Cannot complete scoring from state '{Status}'. Expected '{AudioProcessingStatus.Scoring}'.");
            }

            Status = AudioProcessingStatus.Completed;
            CompletedAt = DateTime.UtcNow;
            FailedStage = null;
            ErrorMessage = null;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Overload tương thích ngược cho các test và component cũ. Điểm chính thức lưu trong QuestionEvaluation.
        /// </summary>
        [Obsolete("Dùng CompleteScoring() không tham số; điểm và feedback lưu trong QuestionEvaluation (D7).")]
        public void CompleteScoring(decimal score, string? feedback = null)
        {
            if (Status != AudioProcessingStatus.Scoring)
            {
                throw new InvalidOperationException($"Cannot complete scoring from state '{Status}'. Expected '{AudioProcessingStatus.Scoring}'.");
            }

#pragma warning disable CS0618
            AiScore = score;
            AiFeedback = feedback;
#pragma warning restore CS0618
            Status = AudioProcessingStatus.Completed;
            CompletedAt = DateTime.UtcNow;
            FailedStage = null;
            ErrorMessage = null;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Ghi nhận lỗi xử lý kèm thông tin giai đoạn bị lỗi (D8).
        /// </summary>
        public void MarkFailed(AudioProcessingStage stage, string errorMessage)
        {
            Status = AudioProcessingStatus.Failed;
            FailedStage = stage;
            ErrorMessage = errorMessage;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Overload tương thích ngược cho TranscriptionWorker và TranscriptionRecoveryService.
        /// </summary>
        public void MarkFailed(string errorMessage)
        {
            MarkFailed(AudioProcessingStage.Transcription, errorMessage);
        }

        /// <summary>
        /// Theo D9: Khi chấm lại thất bại nhưng trước đó câu trả lời đã có bản đánh giá hợp lệ,
        /// trạng thái của InterviewAnswer quay về Completed để bảo toàn kết quả cũ.
        /// </summary>
        public void RevertToCompletedAfterFailedReevaluation(string errorMessage)
        {
            Status = AudioProcessingStatus.Completed;
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

using System.Collections.Generic;

namespace EmployeeHRMS.Api.Configuration
{
    /// <summary>
    /// Cấu hình các chỉ số nhịp nói và phát hiện khoảng lặng/ảo giác (Phase 4).
    /// </summary>
    public class SpeechMetricsOptions
    {
        public const string SectionName = "SpeechMetrics";

        /// <summary>
        /// Ngưỡng trên tốc độ nói (Words Per Minute) để gắn cờ nhịp đọc nhanh bất thường (ước lượng ban đầu: 210 WPM).
        /// </summary>
        public double WpmThresholdHigh { get; set; } = 210.0;

        /// <summary>
        /// Ngưỡng dưới tốc độ nói để phát hiện nói ngập ngừng/quá chậm (ước lượng ban đầu: 90 WPM).
        /// </summary>
        public double WpmThresholdLow { get; set; } = 90.0;

        /// <summary>
        /// Ngưỡng hệ số biến thiên độ dài khoảng lặng giữa các từ (Standard Deviation / Mean).
        /// Giá trị nhỏ (< 0.18) chỉ báo các khoảng nghỉ giữa các từ đều nhau bất thường (nhịp máy đọc hoặc đọc văn bản).
        /// </summary>
        public double PauseVariationCvThreshold { get; set; } = 0.18;

        /// <summary>
        /// Số giây khoảng lặng tối đa ở đầu và cuối bản ghi âm cần cắt bỏ trước khi tính thời lượng lời nói thực tế.
        /// </summary>
        public double TrimSilenceHeadTailSeconds { get; set; } = 1.0;

        /// <summary>
        /// Ngưỡng xác suất không có tiếng nói (no_speech_prob) từ Whisper segment để nhận diện im lặng.
        /// </summary>
        public double MaxNoSpeechProbability { get; set; } = 0.8;

        /// <summary>
        /// Danh sách các cụm từ ảo giác phổ biến khi người dùng im lặng (Whisper Hallucination Phrases).
        /// Nếu transcript chỉ chứa các cụm từ này thì câu trả lời được phân loại NoResponse (Reason = KnownHallucination).
        /// </summary>
        public List<string> KnownHallucinationPhrases { get; set; } = new()
        {
            "Cảm ơn các bạn đã theo dõi",
            "Cảm ơn các bạn đã lắng nghe",
            "Cảm ơn đã xem",
            "Thank you for watching",
            "Thanks for watching",
            "Subtitles by",
            "Hẹn gặp lại",
            "Like and subscribe",
            "Hãy like và subscribe",
            "Chúc các bạn một ngày tốt lành"
        };
    }
}

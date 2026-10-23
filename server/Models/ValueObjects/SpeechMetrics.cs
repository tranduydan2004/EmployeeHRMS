using System;

namespace EmployeeHRMS.Api.Models.ValueObjects
{
    /// <summary>
    /// Value Object lưu các chỉ số nhịp nói thuần túy (WPM, pause variance, v.v.).
    /// Map sang cột jsonb qua EF Core 8 ToJson() Owned Entity.
    /// Tuyệt đối KHÔNG chứa nhãn suy diễn chủ quan (như "cheating" hay "script reading").
    /// </summary>
    public class SpeechMetrics
    {
        /// <summary>
        /// Tốc độ nói tính bằng Words Per Minute.
        /// </summary>
        public double Wpm { get; set; }

        /// <summary>
        /// Thời lượng lời nói thực tế (tính từ timestamp từ đầu tiên đến từ cuối cùng sau khi cắt silence).
        /// </summary>
        public double SpeechDurationSeconds { get; set; }

        /// <summary>
        /// Tổng số từ được phát hiện.
        /// </summary>
        public int WordCount { get; set; }

        /// <summary>
        /// Hệ số biến thiên khoảng lặng (Standard Deviation / Mean của các khoảng nghỉ giữa các từ).
        /// Dùng để so sánh độ biến thiên nhịp điệu giữa người nói nhanh và người nói chậm.
        /// </summary>
        public double PauseVariationCv { get; set; }

        /// <summary>
        /// Độ dài trung bình của các khoảng lặng giữa các từ (tính bằng giây).
        /// </summary>
        public double AveragePauseSeconds { get; set; }

        /// <summary>
        /// Số khoảng lặng giữa các từ vượt ngưỡng tối thiểu (ví dụ > 0.2s).
        /// </summary>
        public int PauseCount { get; set; }

        /// <summary>
        /// Cờ chỉ báo khách quan: nhịp nghỉ giữa các từ có độ đều bất thường hay không (dựa trên ngưỡng cấu hình).
        /// </summary>
        public bool IsRhythmMonotonous { get; set; }

        /// <summary>
        /// Thời điểm tính toán chỉ số.
        /// </summary>
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    }
}

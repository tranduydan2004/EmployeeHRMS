namespace EmployeeHRMS.Api.Configuration
{
    /// <summary>
    /// Cấu hình cho pipeline chấm điểm AI theo rubric và tổng hợp kết quả phỏng vấn (Phase 4).
    /// </summary>
    public class ScoringOptions
    {
        public const string SectionName = "Scoring";

        /// <summary>
        /// Tên model OpenAI dùng riêng cho tác vụ chấm điểm (mặc định: gpt-4o-mini).
        /// </summary>
        public string ModelName { get; set; } = "gpt-4o-mini";

        /// <summary>
        /// Phiên bản prompt chấm điểm hiện hành (dùng cho audit trail).
        /// </summary>
        public string PromptVersion { get; set; } = "v1.0";

        /// <summary>
        /// Nhiệt độ lấy mẫu (0.0 - 1.0). Giá trị thấp (0.1 - 0.2) giúp model chấm nhất quán, bám sát rubric.
        /// </summary>
        public float Temperature { get; set; } = 0.2f;

        /// <summary>
        /// Giới hạn token đầu ra tối đa cho JSON chấm điểm.
        /// </summary>
        public int MaxTokens { get; set; } = 1200;

        /// <summary>
        /// Số lần retry mức HTTP (mạng/rate-limit/5xx).
        /// </summary>
        public int MaxHttpRetries { get; set; } = 3;

        /// <summary>
        /// Số lần retry ngữ nghĩa (khi output JSON không parse được hoặc schema validation thất bại).
        /// </summary>
        public int MaxSemanticRetries { get; set; } = 2;

        /// <summary>
        /// Điểm số quy đổi cho mức Poor (mặc định: 2.5m).
        /// </summary>
        public decimal PoorScore { get; set; } = 2.5m;

        /// <summary>
        /// Điểm số quy đổi cho mức Acceptable (mặc định: 5.0m).
        /// </summary>
        public decimal AcceptableScore { get; set; } = 5.0m;

        /// <summary>
        /// Điểm số quy đổi cho mức Good (mặc định: 7.5m).
        /// </summary>
        public decimal GoodScore { get; set; } = 7.5m;

        /// <summary>
        /// Điểm số quy đổi cho mức Excellent (mặc định: 10.0m).
        /// </summary>
        public decimal ExcellentScore { get; set; } = 10.0m;

        /// <summary>
        /// Ngưỡng điểm trung bình để đạt đề xuất Pass (ước lượng ban đầu: >= 7.0/10).
        /// </summary>
        public decimal PassThreshold { get; set; } = 7.0m;

        /// <summary>
        /// Ngưỡng điểm trung bình để đạt đề xuất Consider (ước lượng ban đầu: >= 5.0/10).
        /// Dưới ngưỡng này là Reject.
        /// </summary>
        public decimal ConsiderThreshold { get; set; } = 5.0m;

        /// <summary>
        /// Ngưỡng điểm tối thiểu cho câu hỏi Technical để không bị Knockout (ước lượng ban đầu: 5.0).
        /// Nếu bất kỳ câu hỏi Technical nào < 5.0 thì kết quả tổng thể bị hạ xuống Consider hoặc Reject.
        /// </summary>
        public decimal TechnicalKnockoutMinScore { get; set; } = 5.0m;

        /// <summary>
        /// Kiểm tra tính hợp lệ của cấu hình thang điểm và các ngưỡng.
        /// </summary>
        public void Validate()
        {
            if (PoorScore < 0 || PoorScore >= AcceptableScore ||
                AcceptableScore >= GoodScore || GoodScore >= ExcellentScore ||
                ExcellentScore > 10.0m)
            {
                throw new System.InvalidOperationException(
                    $"Scoring options levels must satisfy: 0 <= Poor ({PoorScore}) < Acceptable ({AcceptableScore}) < Good ({GoodScore}) < Excellent ({ExcellentScore}) <= 10.0");
            }

            if (PassThreshold < ConsiderThreshold)
            {
                throw new System.InvalidOperationException(
                    $"PassThreshold ({PassThreshold}) cannot be lower than ConsiderThreshold ({ConsiderThreshold}).");
            }
        }
    }
}

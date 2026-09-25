namespace EmployeeHRMS.Api.Configuration
{
    /// <summary>
    /// Cấu hình kết nối OpenAI API — bind từ appsettings via Options Pattern.
    /// Validate on startup: nếu thiếu ApiKey hoặc Model, ứng dụng sẽ fail ngay khi khởi động
    /// thay vì crash lúc runtime khi HR bấm sinh JD.
    /// </summary>
    public class OpenAiSettings
    {
        public const string SectionName = "OpenAiSettings";

        /// <summary>API Key từ OpenAI Platform (sk-...).</summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>Model dùng cho Chat Completions (vd: gpt-4o-mini, gpt-4o).</summary>
        public string Model { get; set; } = "gpt-4o-mini";

        /// <summary>
        /// Temperature cho JD generation (0.0 - 1.0).
        /// Giá trị thấp (0.3-0.5) cho kết quả nhất quán, ít sáng tạo — phù hợp JD chuẩn doanh nghiệp.
        /// </summary>
        public float JdTemperature { get; set; } = 0.4f;

        /// <summary>
        /// Temperature cho Question Bank generation (0.0 - 1.0).
        /// Cao hơn JD một chút (0.5-0.7) để câu hỏi đa dạng hơn.
        /// </summary>
        public float QuestionTemperature { get; set; } = 0.5f;

        /// <summary>Số lần retry tối đa khi LLM trả về JSON không hợp lệ.</summary>
        public int MaxRetries { get; set; } = 2;
    }
}

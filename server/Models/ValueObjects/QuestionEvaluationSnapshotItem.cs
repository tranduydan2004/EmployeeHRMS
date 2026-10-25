namespace EmployeeHRMS.Api.Models.ValueObjects
{
    /// <summary>
    /// Value Object lưu snapshot kết quả đánh giá của từng câu hỏi tại thời điểm HR ra quyết định sơ loại.
    /// Lưu vào cột JSONB trong bảng ScreeningDecisions để đảm bảo tính bất biến (Audit Trail).
    /// </summary>
    public class QuestionEvaluationSnapshotItem
    {
        public int InterviewQuestionId { get; set; }
        public int? EvaluationId { get; set; }
        public int? VersionNumber { get; set; }
        public string? Level { get; set; }
        public decimal? Score { get; set; }
        public string? Outcome { get; set; }
    }
}

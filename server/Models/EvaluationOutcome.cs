namespace EmployeeHRMS.Api.Models
{
    /// <summary>
    /// Kết quả của một lần đánh giá câu trả lời.
    /// Succeeded: Chấm điểm thành công có Level và Score.
    /// NotScorable: Câu hỏi thiếu rubric, câu trả lời lạc đề (OffTopic), hoặc không thể chấm.
    /// Failed: Lỗi kỹ thuật (LLM timeout, JSON hỏng, rate limit quá số lần retry).
    /// NoResponse: Không có câu trả lời (Silence, KnownHallucination).
    /// </summary>
    public enum EvaluationOutcome
    {
        Succeeded = 1,
        NotScorable = 2,
        Failed = 3,
        NoResponse = 4
    }
}

namespace EmployeeHRMS.Api.Models
{
    /// <summary>
    /// Đề xuất kết quả phỏng vấn do code quy định dựa trên điểm trung bình và knockout rules.
    /// Dùng cho HR tham khảo, không thay thế quyết định tuyển dụng cuối cùng của con người.
    /// </summary>
    public enum InterviewRecommendation
    {
        Pass = 1,
        Consider = 2,
        Reject = 3
    }
}

using System;
using System.Collections.Generic;
using EmployeeHRMS.Api.DTOs.Common;
using EmployeeHRMS.Api.Models;

namespace EmployeeHRMS.Api.DTOs
{
    /// <summary>
    /// DTO tối ưu hóa cho màn hình danh sách phỏng vấn sơ loại (Dashboard List View).
    /// Chỉ chiếu các cột cần thiết qua EF Core LINQ .Select(), tuyệt đối KHÔNG nạp
    /// transcript lớn hay jsonb segments vào bộ nhớ.
    /// </summary>
    public class InterviewDashboardSummaryDto
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public string CandidateName { get; set; } = string.Empty;
        public string CandidateEmail { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public int? JobPostingId { get; set; }
        public int? InterviewerId { get; set; }
        public string? InterviewerName { get; set; }
        public DateTime ScheduledDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ApplicationStatus { get; set; } = string.Empty;
        public decimal? AiOverallScore { get; set; }
        public string? Recommendation { get; set; }
        public int TotalQuestions { get; set; }
        public int ScoredQuestions { get; set; }
        public bool HasDecision { get; set; }
        public string? LatestDecisionType { get; set; }
        public DateTime? LatestDecisionAt { get; set; }
    }

    /// <summary>
    /// Tham số lọc và phân trang nâng cao cho Dashboard phỏng vấn sơ loại.
    /// </summary>
    public class InterviewDashboardFilterParams : PaginationParams
    {
        public int? JobPostingId { get; set; }
        public string? Recommendation { get; set; }
        public InterviewStatus? Status { get; set; }
        public string? DecisionStatus { get; set; }
        public int? InterviewerId { get; set; }
    }

    /// <summary>
    /// Thống kê tổng quan nhanh ở đầu Dashboard (Executive Summary).
    /// </summary>
    public class InterviewDashboardStatsDto
    {
        public int TotalInterviews { get; set; }
        public int PendingOrInProgress { get; set; }
        public int Completed { get; set; }
        public int RecommendedPass { get; set; }
        public int RecommendedConsider { get; set; }
        public int RecommendedReject { get; set; }
        public int PendingDecisions { get; set; }
        public int Decided { get; set; }
    }

    /// <summary>
    /// DTO cho danh sách lựa chọn Interviewer phục vụ phân công.
    /// </summary>
    public class InterviewerOptionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}

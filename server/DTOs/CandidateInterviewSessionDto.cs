using System;
using System.Collections.Generic;

namespace EmployeeHRMS.Api.DTOs
{
    public class CandidateInterviewSessionResponseDto
    {
        public Guid InviteToken { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string CandidateName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? InviteExpiresAt { get; set; }
        public DateTime? ConsentedAt { get; set; }
        public int TotalQuestions { get; set; }
        public List<CandidateQuestionDto> Questions { get; set; } = new();
    }

    public class CandidateQuestionDto
    {
        public int Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public int ThinkSeconds { get; set; }
        public int AnswerSeconds { get; set; }
        public bool HasAnswer { get; set; }
    }

    public class CandidateInterviewStartResponseDto
    {
        public Guid InviteToken { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? ConsentedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

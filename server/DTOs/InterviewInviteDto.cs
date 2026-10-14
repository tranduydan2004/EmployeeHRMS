using System;
using System.ComponentModel.DataAnnotations;

namespace EmployeeHRMS.Api.DTOs
{
    public class InterviewInviteCreateDto
    {
        [Range(1, 30, ErrorMessage = "Invite validity must be between 1 and 30 days.")]
        public int? ValidityDays { get; set; }

        [MaxLength(500, ErrorMessage = "Note cannot exceed 500 characters.")]
        public string? Note { get; set; }

        public bool SendEmail { get; set; } = true;
    }

    public class InterviewInviteResponseDto
    {
        public int InterviewId { get; set; }
        public int ApplicationId { get; set; }
        public Guid InviteToken { get; set; }
        public string InviteUrl { get; set; } = string.Empty;
        public DateTime InviteExpiresAt { get; set; }
        public DateTime ScheduledDate { get; set; }
        public int TotalQuestions { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CandidateName { get; set; } = string.Empty;
        public string CandidateEmail { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
    }
}

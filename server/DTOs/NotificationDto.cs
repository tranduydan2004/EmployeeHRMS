using System;
using System.Collections.Generic;

namespace EmployeeHRMS.Api.DTOs
{
    public class NotificationResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string TemplateKey { get; set; } = string.Empty;
        public Dictionary<string, string> Payload { get; set; } = new();
        public string? Message { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? RelatedEntity { get; set; }
        public int? RelatedEntityId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }

    public class UnreadNotificationCountDto
    {
        public int Count { get; set; }
    }
}

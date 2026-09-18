using System;
using System.Collections.Generic;

namespace EmployeeHRMS.Api.Models
{
    public enum NotificationType
    {
        Info = 1,
        Success = 2,
        Warning = 3,
        ActionRequired = 4
    }

    public class Notification
    {
        public int Id { get; private set; }
        public int UserId { get; private set; }
        public ApplicationUser? User { get; private set; }

        public string Title { get; private set; } = string.Empty;
        public string TemplateKey { get; private set; } = string.Empty;
        public Dictionary<string, string> Payload { get; private set; } = new();
        public string? Message { get; private set; }
        public NotificationType Type { get; private set; } = NotificationType.Info;

        public string? RelatedEntity { get; private set; }
        public int? RelatedEntityId { get; private set; }

        public bool IsRead { get; private set; } = false;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; private set; }

        private Notification() { } // Required by EF Core

        public Notification(
            int userId,
            string title,
            string templateKey,
            Dictionary<string, string>? payload = null,
            NotificationType type = NotificationType.Info,
            string? relatedEntity = null,
            int? relatedEntityId = null,
            string? fallbackMessage = null)
        {
            UserId = userId;
            Title = title ?? throw new ArgumentNullException(nameof(title));
            TemplateKey = templateKey ?? throw new ArgumentNullException(nameof(templateKey));
            Payload = payload ?? new Dictionary<string, string>();
            Type = type;
            RelatedEntity = relatedEntity;
            RelatedEntityId = relatedEntityId;
            Message = fallbackMessage;
            CreatedAt = DateTime.UtcNow;
            IsRead = false;
        }

        // Backward compatibility constructor for legacy messages
        public Notification(
            int userId,
            string title,
            string message,
            NotificationType type = NotificationType.Info,
            string? relatedEntity = null,
            int? relatedEntityId = null)
            : this(userId, title, "LEGACY_MESSAGE", new Dictionary<string, string> { { "message", message ?? string.Empty } }, type, relatedEntity, relatedEntityId, message)
        {
        }

        public void MarkAsRead()
        {
            if (!IsRead)
            {
                IsRead = true;
                ReadAt = DateTime.UtcNow;
            }
        }
    }
}

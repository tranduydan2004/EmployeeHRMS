using System.Collections.Generic;
using System.Threading.Tasks;
using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.DTOs.Common;
using EmployeeHRMS.Api.Models;

namespace EmployeeHRMS.Api.Services
{
    public interface INotificationService
    {
        Task<NotificationResponseDto> SendToUserAsync(
            int userId,
            string title,
            string templateKey,
            Dictionary<string, string>? payload = null,
            NotificationType type = NotificationType.Info,
            string? relatedEntity = null,
            int? relatedEntityId = null,
            string? fallbackMessage = null);

        Task SendToRolesAsync(
            IEnumerable<UserRole> roles,
            string title,
            string templateKey,
            Dictionary<string, string>? payload = null,
            NotificationType type = NotificationType.Info,
            string? relatedEntity = null,
            int? relatedEntityId = null,
            string? fallbackMessage = null);

        Task<PagedResult<NotificationResponseDto>> GetUserNotificationsAsync(
            int userId,
            int page = 1,
            int pageSize = 20,
            bool? unreadOnly = null);

        Task<int> GetUnreadCountAsync(int userId);

        Task MarkAsReadAsync(int notificationId, int userId);

        Task MarkAllAsReadAsync(int userId);
    }
}

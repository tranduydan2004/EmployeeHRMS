using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EmployeeHRMS.Api.Data;
using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.DTOs.Common;
using EmployeeHRMS.Api.Exceptions;
using EmployeeHRMS.Api.Hubs;
using EmployeeHRMS.Api.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EmployeeHRMS.Api.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            AppDbContext context,
            IHubContext<NotificationHub> hubContext,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<NotificationResponseDto> SendToUserAsync(
            int userId,
            string title,
            string templateKey,
            Dictionary<string, string>? payload = null,
            NotificationType type = NotificationType.Info,
            string? relatedEntity = null,
            int? relatedEntityId = null,
            string? fallbackMessage = null)
        {
            var notification = new Notification(
                userId,
                title,
                templateKey,
                payload,
                type,
                relatedEntity,
                relatedEntityId,
                fallbackMessage);

            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();

            var dto = new NotificationResponseDto
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Title = notification.Title,
                TemplateKey = notification.TemplateKey,
                Payload = notification.Payload,
                Message = notification.Message,
                Type = notification.Type.ToString(),
                RelatedEntity = notification.RelatedEntity,
                RelatedEntityId = notification.RelatedEntityId,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt,
                ReadAt = notification.ReadAt
            };

            // Real-time Push qua SignalR (bọc try-catch không làm sập flow nghiệp vụ)
            try
            {
                await _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", dto);
                _logger.LogInformation("SignalR notification pushed to user {UserId}: {Title}", userId, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to push SignalR notification to user {UserId}", userId);
            }

            return dto;
        }

        public async Task SendToRolesAsync(
            IEnumerable<UserRole> roles,
            string title,
            string templateKey,
            Dictionary<string, string>? payload = null,
            NotificationType type = NotificationType.Info,
            string? relatedEntity = null,
            int? relatedEntityId = null,
            string? fallbackMessage = null)
        {
            var targetRoles = roles.ToList();
            var targetUsers = await _context.Users
                .Where(u => targetRoles.Contains(u.Role))
                .Select(u => new { u.Id, u.Role })
                .ToListAsync();

            if (!targetUsers.Any())
            {
                _logger.LogInformation("No users found for roles: {Roles}", string.Join(", ", targetRoles));
                return;
            }

            var notifications = targetUsers.Select(u => new Notification(
                u.Id,
                title,
                templateKey,
                payload,
                type,
                relatedEntity,
                relatedEntityId,
                fallbackMessage)).ToList();

            await _context.Notifications.AddRangeAsync(notifications);
            await _context.SaveChangesAsync();

            // Real-time Push tới từng user group đã lưu
            try
            {
                foreach (var n in notifications)
                {
                    var dto = new NotificationResponseDto
                    {
                        Id = n.Id,
                        UserId = n.UserId,
                        Title = n.Title,
                        TemplateKey = n.TemplateKey,
                        Payload = n.Payload,
                        Message = n.Message,
                        Type = n.Type.ToString(),
                        RelatedEntity = n.RelatedEntity,
                        RelatedEntityId = n.RelatedEntityId,
                        IsRead = n.IsRead,
                        CreatedAt = n.CreatedAt,
                        ReadAt = n.ReadAt
                    };

                    await _hubContext.Clients.Group($"User_{n.UserId}").SendAsync("ReceiveNotification", dto);
                }

                _logger.LogInformation("SignalR notifications pushed to {Count} users in roles: {Roles}",
                    notifications.Count, string.Join(", ", targetRoles));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast SignalR notifications to roles: {Roles}", string.Join(", ", targetRoles));
            }
        }

        public async Task<PagedResult<NotificationResponseDto>> GetUserNotificationsAsync(
            int userId,
            int page = 1,
            int pageSize = 20,
            bool? unreadOnly = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var query = _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId);

            if (unreadOnly == true)
            {
                query = query.Where(n => !n.IsRead);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationResponseDto
                {
                    Id = n.Id,
                    UserId = n.UserId,
                    Title = n.Title,
                    TemplateKey = n.TemplateKey,
                    Payload = n.Payload,
                    Message = n.Message,
                    Type = n.Type.ToString(),
                    RelatedEntity = n.RelatedEntity,
                    RelatedEntityId = n.RelatedEntityId,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    ReadAt = n.ReadAt
                })
                .ToListAsync();

            return new PagedResult<NotificationResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .AsNoTracking()
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task MarkAsReadAsync(int notificationId, int userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy thông báo #{notificationId}.");
            }

            if (notification.UserId != userId)
            {
                throw new BusinessRuleException("Bạn không có quyền thao tác trên thông báo này.", HttpStatusCode.Forbidden);
            }

            notification.MarkAsRead();
            await _context.SaveChangesAsync();
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (unreadNotifications.Any())
            {
                foreach (var n in unreadNotifications)
                {
                    n.MarkAsRead();
                }

                await _context.SaveChangesAsync();
            }
        }
    }
}

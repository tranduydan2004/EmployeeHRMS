using System.Threading.Tasks;
using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.DTOs.Common;
using EmployeeHRMS.Api.Extensions;
using EmployeeHRMS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeHRMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // GET: api/notifications?page=1&pageSize=20&unreadOnly=false
        [HttpGet]
        public async Task<ActionResult<PagedResult<NotificationResponseDto>>> GetNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool? unreadOnly = null)
        {
            var userId = User.GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { error = "Không thể xác thực người dùng." });
            }

            var result = await _notificationService.GetUserNotificationsAsync(userId.Value, page, pageSize, unreadOnly);
            return Ok(result);
        }

        // GET: api/notifications/unread-count
        [HttpGet("unread-count")]
        public async Task<ActionResult<UnreadNotificationCountDto>> GetUnreadCount()
        {
            var userId = User.GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { error = "Không thể xác thực người dùng." });
            }

            var count = await _notificationService.GetUnreadCountAsync(userId.Value);
            return Ok(new UnreadNotificationCountDto { Count = count });
        }

        // PATCH: api/notifications/{id}/read
        [HttpPatch("{id:int}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = User.GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { error = "Không thể xác thực người dùng." });
            }

            await _notificationService.MarkAsReadAsync(id, userId.Value);
            return NoContent();
        }

        // PATCH: api/notifications/read-all
        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { error = "Không thể xác thực người dùng." });
            }

            await _notificationService.MarkAllAsReadAsync(userId.Value);
            return NoContent();
        }
    }
}

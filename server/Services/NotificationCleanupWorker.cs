using System;
using System.Threading;
using System.Threading.Tasks;
using EmployeeHRMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Hosted Service chạy ngầm định kỳ mỗi 24 giờ để dọn dẹp các thông báo cũ:
    /// 1. Đã đọc + cũ hơn 30 ngày.
    /// 2. Chưa đọc + cũ hơn 90 ngày.
    /// 3. Thông báo lịch phỏng vấn + cũ hơn 7 ngày.
    /// Dùng ExecuteDeleteAsync để xóa trực tiếp trên PostgreSQL, không tải entities vào bộ nhớ.
    /// </summary>
    public class NotificationCleanupWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationCleanupWorker> _logger;
        private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(24);

        public NotificationCleanupWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<NotificationCleanupWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificationCleanupWorker đã khởi động.");

            using var timer = new PeriodicTimer(CleanupInterval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanStaleNotificationsAsync(stoppingToken);
                    await timer.WaitForNextTickAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("NotificationCleanupWorker đang dừng theo yêu cầu hủy.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi xảy ra trong quá trình dọn dẹp Notification định kỳ.");
                    try
                    {
                        await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        private async Task CleanStaleNotificationsAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.UtcNow;
            var readCutoff = now.AddDays(-30);
            var unreadCutoff = now.AddDays(-90);
            var interviewCutoff = now.AddDays(-7);

            // Quy tắc 1: Đã đọc + cũ hơn 30 ngày
            var deletedRead = await context.Notifications
                .Where(n => n.IsRead && n.CreatedAt < readCutoff)
                .ExecuteDeleteAsync(stoppingToken);

            // Quy tắc 2: Chưa đọc + cũ hơn 90 ngày
            var deletedUnread = await context.Notifications
                .Where(n => !n.IsRead && n.CreatedAt < unreadCutoff)
                .ExecuteDeleteAsync(stoppingToken);

            // Quy tắc 3: Thông báo lịch phỏng vấn + cũ hơn 7 ngày
            var deletedInterview = await context.Notifications
                .Where(n => (n.TemplateKey == "INTERVIEW_SCHEDULED"
                             || n.TemplateKey == "INTERVIEW_SCHEDULED_CANDIDATE"
                             || n.TemplateKey == "INTERVIEW_ASSIGNED_INTERVIEWER")
                            && n.CreatedAt < interviewCutoff)
                .ExecuteDeleteAsync(stoppingToken);

            var totalDeleted = deletedRead + deletedUnread + deletedInterview;
            if (totalDeleted > 0)
            {
                _logger.LogInformation(
                    "Đã dọn dẹp {Total} thông báo (Đã đọc >30d: {Read}, Chưa đọc >90d: {Unread}, Phỏng vấn >7d: {Interview}).",
                    totalDeleted, deletedRead, deletedUnread, deletedInterview);
            }
        }
    }
}

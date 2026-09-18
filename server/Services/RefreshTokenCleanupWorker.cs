using EmployeeHRMS.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Hosted Service chạy ngầm định kỳ mỗi 24 giờ để dọn dẹp các refresh token đã hết hạn hoặc đã thu hồi quá 30 ngày.
    /// </summary>
    public class RefreshTokenCleanupWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RefreshTokenCleanupWorker> _logger;
        private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(24);

        public RefreshTokenCleanupWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<RefreshTokenCleanupWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RefreshTokenCleanupWorker đã khởi động.");

            using var timer = new PeriodicTimer(CleanupInterval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanStaleTokensAsync(stoppingToken);
                    await timer.WaitForNextTickAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("RefreshTokenCleanupWorker đang dừng theo yêu cầu hủy.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi xảy ra trong quá trình dọn dẹp RefreshToken định kỳ.");
                    // Đợi 1 tiếng trước khi thử lại nếu xảy ra lỗi
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

        private async Task CleanStaleTokensAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.UtcNow;
            var cutoffDate = now.AddDays(-30);

            var deletedCount = await context.RefreshTokens
                .Where(rt => (rt.RevokedAt != null || rt.ExpiresAt < now) && rt.CreatedAt < cutoffDate)
                .ExecuteDeleteAsync(stoppingToken);

            if (deletedCount > 0)
            {
                _logger.LogInformation("Đã dọn dẹp {Count} refresh token cũ hơn 30 ngày.", deletedCount);
            }
        }
    }
}

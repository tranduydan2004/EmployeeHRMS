using System.Diagnostics;

namespace EmployeeHRMS.Api.Middleware
{
    /// <summary>
    /// Custom Middleware — minh họa: ASP.NET Core Middleware Pipeline
    /// Log mọi HTTP request: Method, Path, StatusCode, Duration
    /// 
    /// Middleware pipeline: Request → [Middleware 1] → [Middleware 2] → ... → [Endpoint]
    ///                      Response ← [Middleware 1] ← [Middleware 2] ← ... ← [Endpoint]
    /// </summary>
    public class RequestLoggingMiddleware
    {
        // RequestDelegate đại diện cho middleware tiếp theo trong pipeline
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        // Constructor injection: nhận RequestDelegate và ILogger từ DI container
        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// InvokeAsync — method chính mà ASP.NET Core sẽ gọi cho mỗi HTTP request.
        /// Minh họa: async/await trong middleware
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            // === TRƯỚC khi request đi vào pipeline ===
            var stopwatch = Stopwatch.StartNew();
            var method = context.Request.Method;
            var path = context.Request.Path;

            _logger.LogInformation(
                "══► [Request] {Method} {Path} started at {Time}",
                method, path, DateTime.UtcNow.ToString("HH:mm:ss.fff"));

            // Gọi middleware tiếp theo trong pipeline (hoặc endpoint cuối cùng)
            // Nếu không gọi _next(), request sẽ bị chặn tại đây (short-circuit)
            await _next(context);

            // === SAU khi response trả về từ pipeline ===
            stopwatch.Stop();

            _logger.LogInformation(
                "◄══ [Response] {Method} {Path} → {StatusCode} ({Duration}ms)",
                method, path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Extension method để đăng ký middleware — clean syntax trong Program.cs
    /// Thay vì: app.UseMiddleware&lt;RequestLoggingMiddleware&gt;()
    /// Dùng:    app.UseRequestLogging()
    /// </summary>
    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}

using System.Net;
using System.Text.Json;
using EmployeeHRMS.Api.Exceptions;

namespace EmployeeHRMS.Api.Middleware
{
    /// <summary>
    /// Global Exception Handling Middleware.
    /// Bắt tất cả exception chưa được xử lý, map sang HTTP status code phù hợp,
    /// và trả về JSON chuẩn format: { "error": "Chi tiết lỗi" }.
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, message) = exception switch
            {
                NotFoundException ex => ((int)HttpStatusCode.NotFound, ex.Message),
                BusinessRuleException ex => ((int)ex.StatusCode, ex.Message),
                ConflictException ex => ((int)HttpStatusCode.Conflict, ex.Message),
                _ => ((int)HttpStatusCode.InternalServerError, GetInternalErrorMessage(exception))
            };

            // Log full stack trace cho lỗi 500
            if (statusCode == (int)HttpStatusCode.InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var response = JsonSerializer.Serialize(
                new { error = message },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            );

            await context.Response.WriteAsync(response);
        }

        /// <summary>
        /// Development: trả exception message chi tiết.
        /// Production: chỉ trả thông báo chung.
        /// </summary>
        private string GetInternalErrorMessage(Exception exception)
        {
            return _env.IsDevelopment()
                ? exception.Message
                : "An unexpected error occurred.";
        }
    }
}

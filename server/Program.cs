using EmployeeHRMS.Api.Helpers;
using EmployeeHRMS.Api.Hubs;
using EmployeeHRMS.Api.Middleware;
using EmployeeHRMS.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Microsoft.EntityFrameworkCore;
using EmployeeHRMS.Api.Data;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

namespace EmployeeHRMS.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ================================================================
            // 1. CONFIGURATION — Đọc settings từ appsettings.json
            // Minh họa: IConfiguration, reading configuration values
            // ================================================================
            var apiName = builder.Configuration["AppSettings:ApiName"] ?? "Employee HRMS API";
            var apiVersion = builder.Configuration["AppSettings:ApiVersion"] ?? "v1";

            // ================================================================
            // 2. SERVICE REGISTRATION (DI Container)
            // Minh họa: Dependency Injection — đăng ký Interface → Implementation
            // ================================================================

            // Add Controllers
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // Serialize enum thành string thay vì số (cho Swagger dễ đọc)
                    options.JsonSerializerOptions.Converters.Add(
                        new System.Text.Json.Serialization.JsonStringEnumConverter());
                });

            // === Cấu hình Validation Response Factory ===
            // Ghi đè InvalidModelStateResponseFactory mặc định (ValidationProblemDetails)
            // để trả về JSON chuẩn format: { "error": "..." }
            builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(e => e.Value?.Errors.Count > 0)
                        .SelectMany(e => e.Value!.Errors.Select(err => err.ErrorMessage))
                        .ToList();

                    var errorMessage = string.Join(" | ", errors);

                    return new BadRequestObjectResult(new { error = errorMessage });
                };
            });

            // Add DbContext with PostgreSQL
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
            );

            // === JWT Authentication ===
            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"]
                ?? throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings["Issuer"],
                        ValidAudience = jwtSettings["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(secretKey)),
                        ClockSkew = TimeSpan.Zero // Không cho phép lệch thời gian
                    };

                    // Trả về JSON { "error": "..." } thay vì WWW-Authenticate header mặc định
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"].FirstOrDefault()
                                              ?? context.Request.Query["accessToken"].FirstOrDefault();
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) &&
                                (path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase) ||
                                 path.Value?.Contains("/hubs/", StringComparison.OrdinalIgnoreCase) == true))
                            {
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        },
                        OnChallenge = async context =>
                        {
                            // Suppress default behavior
                            context.HandleResponse();
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsync(
                                System.Text.Json.JsonSerializer.Serialize(
                                    new { error = "Authentication required. Please provide a valid Bearer token." }));
                        },
                        OnForbidden = async context =>
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsync(
                                System.Text.Json.JsonSerializer.Serialize(
                                    new { error = "You do not have permission to access this resource." }));
                        }
                    };
                });

            builder.Services.AddAuthorization();

            // === CORS — FrontendClient policy ===
            var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:5173" };

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("FrontendClient", policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .SetIsOriginAllowed(origin =>
                          {
                              if (string.IsNullOrEmpty(origin)) return false;
                              try
                              {
                                  var uri = new Uri(origin);
                                  return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                                         uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);
                              }
                              catch
                              {
                                  return false;
                              }
                          })
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            // === Rate Limiting ===
            builder.Services.AddRateLimiter(options =>
            {
                // AuthRateLimit — 5 requests/phút/IP (chống brute-force login)
                options.AddFixedWindowLimiter("AuthRateLimit", opt =>
                {
                    opt.PermitLimit = 5;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 0;
                });

                // ForgotPasswordRateLimit — 3 requests/giờ/IP (chống spam email)
                options.AddFixedWindowLimiter("ForgotPasswordRateLimit", opt =>
                {
                    opt.PermitLimit = 3;
                    opt.Window = TimeSpan.FromHours(1);
                    opt.QueueLimit = 0;
                });

                // ResendVerificationRateLimit — 3 requests/giờ/IP (chống spam email)
                options.AddFixedWindowLimiter("ResendVerificationRateLimit", opt =>
                {
                    opt.PermitLimit = 3;
                    opt.Window = TimeSpan.FromHours(1);
                    opt.QueueLimit = 0;
                });

                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = 429;
                    context.HttpContext.Response.ContentType = "application/json";
                    await context.HttpContext.Response.WriteAsync(
                        "{ \"error\": \"Quá nhiều yêu cầu. Vui lòng thử lại sau.\" }", token);
                };
            });

            // Swagger / OpenAPI — với JWT Bearer Security
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = apiName,
                    Version = apiVersion,
                    Description = "Employee HRMS API — Hệ thống quản lý nhân sự. " +
                                  "Built with ASP.NET Core 8, JWT Authentication, Role-Based Authorization."
                });

                // Thêm nút "Authorize" trên Swagger UI
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Enter your JWT token. Example: eyJhbGciOi...",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT"
                });

                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
                });
            });

            // === Đăng ký Services — Scoped (EF Core + PostgreSQL) ===

            // Helper services
            builder.Services.AddSingleton<EventLogger>();

            // Auth services
            builder.Services.AddScoped<IJwtService, JwtService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddHostedService<RefreshTokenCleanupWorker>();
            builder.Services.AddHostedService<NotificationCleanupWorker>();

            // HttpContextAccessor — cần cho query-level filtering trong Service
            builder.Services.AddHttpContextAccessor();

            // File storage & validation services
            builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
            builder.Services.AddScoped<IFileValidatorService, FileValidatorService>();

            // Email service
            builder.Services.AddScoped<IEmailService, MailKitEmailService>();

            // User provisioning service (Onboarding Employee)
            builder.Services.AddScoped<UserProvisioningService>();

            // Authorization handlers — Resource-Based Authorization
            builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler,
                EmployeeHRMS.Api.Authorization.CandidateAuthorizationHandler>();
            builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler,
                EmployeeHRMS.Api.Authorization.ApplicationAuthorizationHandler>();
            builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler,
                EmployeeHRMS.Api.Authorization.InterviewAuthorizationHandler>();
            builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler,
                EmployeeHRMS.Api.Authorization.EmployeeAuthorizationHandler>();

            // Business services — Interface → Implementation (DI pattern)
            builder.Services.AddScoped<IDepartmentService, DepartmentService>();
            builder.Services.AddScoped<IEmployeeService, EmployeeService>();
            builder.Services.AddScoped<IJobPostingService, JobPostingService>();
            builder.Services.AddScoped<ICandidateService, CandidateService>();
            builder.Services.AddScoped<IApplicationService, ApplicationService>();
            builder.Services.AddScoped<IInterviewService, InterviewService>();

            // SignalR & Real-time Notification Service
            builder.Services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });
            builder.Services.AddScoped<INotificationService, NotificationService>();

            var app = builder.Build();

            // ================================================================
            // 3. MIDDLEWARE PIPELINE — Thứ tự middleware RẤT QUAN TRỌNG
            // Request đi từ trên xuống, Response đi từ dưới lên
            // ================================================================

            // [Middleware 1] Swagger UI — chỉ bật trong Development (đặt trước Exception handler)
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", $"{apiName} {apiVersion}");
                });
            }

            // [Middleware 2] Global Exception Handling — bắt tất cả exception chưa xử lý
            app.UseMiddleware<GlobalExceptionMiddleware>();

            // [Middleware 3] HTTPS Redirection — chỉ bật ngoài Development để tránh 307 redirect handshake WebSocket
            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            // [Middleware 4] CORS — TRƯỚC WebSockets và RateLimiter
            app.UseCors("FrontendClient");

            // [Middleware 4.1] WebSockets Middleware
            app.UseWebSockets();

            // [Middleware 5] Rate Limiting — SAU UseCors, TRƯỚC UseAuthentication
            app.UseRateLimiter();

            // [Middleware 6] Authentication — phải đặt TRƯỚC Authorization
            app.UseAuthentication();

            // [Middleware 7] Authorization
            app.UseAuthorization();

            // [Endpoint] Map Controllers — terminal middleware
            app.MapControllers();

            // [Endpoint] Map SignalR Hub
            app.MapHub<NotificationHub>("/hubs/notifications");

            // ================================================================
            // 4. RUN APPLICATION
            // ================================================================
            app.Run();
        }
    }
}

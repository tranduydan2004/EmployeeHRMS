using MailKit.Net.Smtp;
using MimeKit;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// MailKit implementation of IEmailService.
    /// Đọc cấu hình từ "EmailSettings" section trong appsettings.json.
    /// Bọc try-catch — log lỗi qua ILogger nếu SMTP thất bại, trả false.
    /// </summary>
    public class MailKitEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MailKitEmailService> _logger;

        public MailKitEmailService(IConfiguration configuration, ILogger<MailKitEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var emailSettings = _configuration.GetSection("EmailSettings");
                var smtpServer = emailSettings["SmtpServer"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(emailSettings["SmtpPort"] ?? "587");
                var senderName = emailSettings["SenderName"] ?? "HRMS System";
                var senderEmail = emailSettings["SenderEmail"] ?? "noreply@hrms.com";
                var smtpUsername = emailSettings["SmtpUsername"] ?? string.Empty;
                var smtpPassword = emailSettings["SmtpPassword"] ?? string.Empty;
                var enableSsl = bool.Parse(emailSettings["EnableSsl"] ?? "true");

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(smtpServer, smtpPort, enableSsl
                    ? MailKit.Security.SecureSocketOptions.StartTls
                    : MailKit.Security.SecureSocketOptions.None);

                if (!string.IsNullOrWhiteSpace(smtpUsername))
                {
                    await client.AuthenticateAsync(smtpUsername, smtpPassword);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {ToEmail}, Subject: {Subject}", toEmail, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}, Subject: {Subject}", toEmail, subject);
                return false;
            }
        }
    }
}

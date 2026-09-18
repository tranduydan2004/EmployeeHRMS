namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Interface cho Email Service — gửi email bất đồng bộ qua SMTP.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Gửi email HTML. Nếu SMTP lỗi, log lỗi và KHÔNG throw exception
        /// (tránh làm sập luồng chính — caller tự handle bằng return value).
        /// </summary>
        /// <returns>true nếu gửi thành công, false nếu thất bại.</returns>
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody);
    }
}

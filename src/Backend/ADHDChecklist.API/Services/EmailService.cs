using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace ADHDChecklist.API.Services;

public interface IEmailService
{
    Task SendEmailVerificationAsync(string toEmail, string userName, string verificationLink);
    Task SendPasswordResetAsync(string toEmail, string userName, string resetLink);
    Task SendWelcomeEmailAsync(string toEmail, string userName);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailVerificationAsync(string toEmail, string userName, string verificationLink)
    {
        var subject = "Xác nhận email - ADHD Checklist";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Xin chào {userName}!</h2>
                <p>Cảm ơn bạn đã đăng ký ADHD Checklist.</p>
                <p>Vui lòng click vào link dưới đây để xác nhận email của bạn:</p>
                <p>
                    <a href='{verificationLink}' 
                       style='background-color: #4F46E5; color: white; padding: 12px 24px; 
                              text-decoration: none; border-radius: 6px; display: inline-block;'>
                        Xác nhận Email
                    </a>
                </p>
                <p>Hoặc copy link này vào trình duyệt:</p>
                <p style='color: #666; font-size: 12px;'>{verificationLink}</p>
                <p style='color: #999; font-size: 11px; margin-top: 30px;'>
                    Link này sẽ hết hạn sau 24 giờ.
                </p>
            </body>
            </html>
        ";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendPasswordResetAsync(string toEmail, string userName, string resetLink)
    {
        var subject = "Đặt lại mật khẩu - ADHD Checklist";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Xin chào {userName}!</h2>
                <p>Bạn đã yêu cầu đặt lại mật khẩu.</p>
                <p>Click vào link dưới đây để đặt lại mật khẩu:</p>
                <p>
                    <a href='{resetLink}' 
                       style='background-color: #EF4444; color: white; padding: 12px 24px; 
                              text-decoration: none; border-radius: 6px; display: inline-block;'>
                        Đặt lại mật khẩu
                    </a>
                </p>
                <p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
                <p style='color: #999; font-size: 11px; margin-top: 30px;'>
                    Link này sẽ hết hạn sau 1 giờ.
                </p>
            </body>
            </html>
        ";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string userName)
    {
        var subject = "Chào mừng đến với ADHD Checklist! 🎉";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Chào mừng {userName}! 🎉</h2>
                <p>Email của bạn đã được xác nhận thành công.</p>
                <h3>Bắt đầu với ADHD Checklist:</h3>
                <ul>
                    <li>✅ Tạo tasks và sắp xếp vào time blocks</li>
                    <li>🔔 Đặt reminders để không bỏ lỡ việc quan trọng</li>
                    <li>📊 Xem analytics để hiểu productivity patterns</li>
                </ul>
                <p>Nâng cấp lên <strong>Premium</strong> để mở khóa:</p>
                <ul>
                    <li>🎯 Focus Mode với white noise</li>
                    <li>💪 Habit Tracker</li>
                    <li>📈 Deep Analytics</li>
                    <li>🎨 Custom themes</li>
                </ul>
                <p style='margin-top: 30px;'>
                    <a href='https://localhost:7002' 
                       style='background-color: #10B981; color: white; padding: 12px 24px; 
                              text-decoration: none; border-radius: 6px; display: inline-block;'>
                        Bắt đầu ngay
                    </a>
                </p>
            </body>
            </html>
        ";

        await SendEmailAsync(toEmail, subject, body);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                _configuration["EmailSettings:SenderName"],
                _configuration["EmailSettings:SenderEmail"]
            ));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _configuration["EmailSettings:SmtpServer"],
                int.Parse(_configuration["EmailSettings:SmtpPort"]!),
                SecureSocketOptions.StartTls
            );

            await smtp.AuthenticateAsync(
                _configuration["EmailSettings:Username"],
                _configuration["EmailSettings:Password"]
            );

            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            throw;
        }
    }
}
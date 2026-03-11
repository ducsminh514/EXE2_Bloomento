using System.Net.Http.Json;

namespace ADHDChecklist.API.Services;

public interface IEmailService
{
    Task SendEmailVerificationAsync(string toEmail, string userName, string verificationLink);
    Task SendPasswordResetAsync(string toEmail, string userName, string resetLink);
    Task SendWelcomeEmailAsync(string toEmail, string userName);
    Task SendEmailAsync(string toEmail, string subject, string htmlBody);
    Task SendFamilyInvitationAsync(string toEmail, string familyName, string inviterName, string code);
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
        var frontendUrl = _configuration["FrontendUrl"] ?? "https://exe-2-bloomento-ducsminh514s-projects.vercel.app";
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
                    <a href='{frontendUrl}' 
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

    public async Task SendFamilyInvitationAsync(string toEmail, string familyName, string inviterName, string code)
    {
        var subject = $"Chào mừng bạn tham gia gia đình {familyName}! 🌸";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <div style='max-width: 600px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden;'>
                    <div style='background-color: #4F46E5; color: white; padding: 24px; text-align: center;'>
                        <h1 style='margin: 0;'>Bloomento Gia đình</h1>
                    </div>
                    <div style='padding: 24px; color: #1e293b;'>
                        <h2 style='color: #4F46E5;'>Xin chào!</h2>
                        <p><strong>{inviterName}</strong> vừa mời bạn tham gia nhóm gia đình <strong>{familyName}</strong> trên Bloomento.</p>
                        <p>Tham gia gia đình giúp cả nhà cùng nhau:</p>
                        <ul style='color: #475569;'>
                            <li>✅ Chia sẻ và hỗ trợ các công việc hàng ngày</li>
                            <li>🏆 Tích điểm thưởng cho các thói quen tốt</li>
                            <li>📊 Theo dõi tiến độ và động viên nhau</li>
                        </ul>
                        <div style='background-color: #f8fafc; border: 1px dashed #cbd5e1; border-radius: 8px; padding: 16px; text-align: center; margin: 24px 0;'>
                            <p style='margin: 0 0 8px 0; font-size: 14px; color: #64748b;'>Mã mời của bạn:</p>
                            <div style='font-family: monospace; font-size: 32px; font-weight: bold; color: #4F46E5; letter-spacing: 4px;'>{code}</div>
                            <p style='margin: 8px 0 0 0; font-size: 11px; color: #94a3b8;'>Mã này sẽ hết hạn sau 7 ngày.</p>
                        </div>
                        <p>Hãy mở ứng dụng Bloomento, chọn <strong>Gia đình</strong> và nhập mã này để tham gia ngay!</p>
                        <p style='margin-top: 24px; font-size: 12px; color: #94a3b8; text-align: center;'>
                            Nếu bạn không biết về lời mời này, vui lòng bỏ qua email.
                        </p>
                    </div>
                </div>
            </body>
            </html>
        ";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            var senderName = _configuration["EmailSettings:SenderName"];
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            var apiKey = _configuration["EmailSettings:Password"];

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("api-key", apiKey);

            var requestBody = new
            {
                sender = new { name = senderName, email = senderEmail },
                to = new[] { new { email = toEmail } },
                subject = subject,
                htmlContent = htmlBody
            };

            var response = await client.PostAsJsonAsync("https://api.brevo.com/v3/smtp/email", requestBody);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent successfully to {Email} via Brevo API", toEmail);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Brevo API Error for {Email}: {StatusCode} - {Content}", toEmail, response.StatusCode, errorContent);
                throw new Exception($"Failed to send email via Brevo: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending email to {Email}", toEmail);
            throw;
        }
    }
}
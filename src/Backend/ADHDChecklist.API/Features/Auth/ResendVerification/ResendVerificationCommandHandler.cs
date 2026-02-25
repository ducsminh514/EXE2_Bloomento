using ADHDChecklist.API.Entities.Common;
using ADHDChecklist.API.Services;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace ADHDChecklist.API.Features.Auth.ResendVerification
{
    public class ResendVerificationCommandHandler : IRequestHandler<ResendVerificationCommand, ResendVerificationResponse>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ResendVerificationCommandHandler> _logger;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public ResendVerificationCommandHandler(
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<ResendVerificationCommandHandler> logger,
            IBackgroundJobClient backgroundJobClient)
        {
            _userManager = userManager;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
            _backgroundJobClient = backgroundJobClient;
        }

        public async Task<ResendVerificationResponse> Handle(ResendVerificationCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
            {
                // Don't reveal that user doesn't exist
                return new ResendVerificationResponse(
                    Success: true,
                    Message: "Nếu email tồn tại trong hệ thống, một email xác nhận mới đã được gửi."
                );
            }

            // Check if already verified
            if (user.IsEmailVerified)
            {
                return new ResendVerificationResponse(
                    Success: false,
                    Message: "Email đã được xác nhận trước đó."
                );
            }

            // Generate new token
            user.EmailVerificationToken = Guid.NewGuid().ToString("N");
            user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);

            await _userManager.UpdateAsync(user);

            // Send verification email
            try
            {
                var frontendUrl = _configuration["FrontendUrl"] ?? "https://localhost:7002";
                var verificationLink = $"{frontendUrl}/verify-email?token={user.EmailVerificationToken}&userId={user.Id}";

                _backgroundJobClient.Enqueue(() =>
                    _emailService.SendEmailVerificationAsync(
                        user.Email!,
                        user.FullName ?? user.Email!,
                        verificationLink
                    )
                );

                _logger.LogInformation("Verification email resent to {Email}", user.Email);

                return new ResendVerificationResponse(
                    Success: true,
                    Message: "Email xác nhận mới đã được gửi. Vui lòng kiểm tra hộp thư."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend verification email to {Email}", user.Email);
                return new ResendVerificationResponse(
                    Success: false,
                    Message: "Có lỗi xảy ra khi gửi email. Vui lòng thử lại sau."
                );
            }
        }
    }

}

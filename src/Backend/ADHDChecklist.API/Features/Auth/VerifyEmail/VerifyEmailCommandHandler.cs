using ADHDChecklist.API.Entities.Common;
using ADHDChecklist.API.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace ADHDChecklist.API.Features.Auth.VerifyEmail
{
    // ============================================
    // HANDLER
    // ============================================
    public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, VerifyEmailResponse>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<VerifyEmailCommandHandler> _logger;

        public VerifyEmailCommandHandler(
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            ILogger<VerifyEmailCommandHandler> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<VerifyEmailResponse> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.UserId, out var userId))
            {
                return new VerifyEmailResponse(
                    Success: false,
                    Message: "Link xác nhận không hợp lệ"
                );
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return new VerifyEmailResponse(
                    Success: false,
                    Message: "Không tìm thấy tài khoản"
                );
            }

            // Check if already verified
            if (user.IsEmailVerified)
            {
                return new VerifyEmailResponse(
                    Success: true,
                    Message: "Email đã được xác nhận trước đó"
                );
            }

            // Validate token
            if (user.EmailVerificationToken != request.Token)
            {
                _logger.LogWarning("Invalid verification token for user {UserId}", userId);
                return new VerifyEmailResponse(
                    Success: false,
                    Message: "Link xác nhận không hợp lệ"
                );
            }

            // Check if token expired
            if (!user.IsEmailVerificationValid())
            {
                _logger.LogWarning("Expired verification token for user {UserId}", userId);
                return new VerifyEmailResponse(
                    Success: false,
                    Message: "Link xác nhận đã hết hạn. Vui lòng yêu cầu gửi lại email xác nhận."
                );
            }

            // Mark as verified
            user.IsEmailVerified = true;
            user.EmailConfirmed = true;
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpiry = null;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                _logger.LogError("Failed to update user {UserId} verification status", userId);
                return new VerifyEmailResponse(
                    Success: false,
                    Message: "Có lỗi xảy ra. Vui lòng thử lại."
                );
            }

            // Send welcome email
            try
            {
                await _emailService.SendWelcomeEmailAsync(
                    user.Email!,
                    user.FullName ?? user.Email!
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
                // Don't fail verification if welcome email fails
            }

            _logger.LogInformation("User {UserId} email verified successfully", userId);

            return new VerifyEmailResponse(
                Success: true,
                Message: "Email đã được xác nhận thành công! Bạn có thể đăng nhập ngay."
            );
        }
    }
}

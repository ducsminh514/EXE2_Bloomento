using ADHDChecklist.API.Entities.Common;
using ADHDChecklist.API.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace ADHDChecklist.API.Features.Auth.Register
{
    // ============================================
    // HANDLER
    // ============================================
    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponse>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RegisterCommandHandler> _logger;

        public RegisterCommandHandler(
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<RegisterCommandHandler> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return new RegisterResponse(
                    Success: false,
                    Message: "Email này đã được đăng ký"
                );
            }

            // Create new user
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                EmailConfirmed = false,
                IsEmailVerified = false,
                SubscriptionTier = SubscriptionTier.Free,
                CreatedAt = DateTime.UtcNow,
                TimeZone = "Asia/Ho_Chi_Minh"
            };

            // Generate email verification token
            user.EmailVerificationToken = Guid.NewGuid().ToString("N");
            user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("User registration failed: {Errors}", errors);

                return new RegisterResponse(
                    Success: false,
                    Message: $"Đăng ký thất bại: {errors}"
                );
            }

            // Send verification email
            try
            {
                var frontendUrl = _configuration["FrontendUrl"];
                var verificationLink = $"{frontendUrl}/verify-email?token={user.EmailVerificationToken}&userId={user.Id}";

                await _emailService.SendEmailVerificationAsync(
                    user.Email!,
                    user.FullName ?? user.Email!,
                    verificationLink
                );

                _logger.LogInformation("User {UserId} registered successfully", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification email to {Email}", user.Email);
                // Don't fail registration if email fails
            }

            return new RegisterResponse(
                Success: true,
                Message: "Đăng ký thành công! Vui lòng kiểm tra email để xác nhận tài khoản.",
                UserId: user.Id.ToString()
            );
        }
    }

}

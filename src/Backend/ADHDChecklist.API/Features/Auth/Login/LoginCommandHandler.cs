using ADHDChecklist.API.Entities.Common;
using ADHDChecklist.API.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace ADHDChecklist.API.Features.Auth.Login
{
    // ============================================
    // HANDLER
    // ============================================
    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ILogger<LoginCommandHandler> _logger;

        public LoginCommandHandler(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtTokenService jwtTokenService,
            ILogger<LoginCommandHandler> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtTokenService = jwtTokenService;
            _logger = logger;
        }

        public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            // Find user by email
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Login attempt with non-existent email: {Email}", request.Email);
                return new LoginResponse(
                    Success: false,
                    Message: "Email hoặc mật khẩu không đúng"
                );
            }

            // Check if account is active
            if (!user.IsActive)
            {
                return new LoginResponse(
                    Success: false,
                    Message: "Tài khoản đã bị khóa. Vui lòng liên hệ hỗ trợ."
                );
            }

            // ✅ CHECK EMAIL VERIFICATION
            if (!user.IsEmailVerified)
            {
                _logger.LogWarning("Login attempt with unverified email: {Email}", request.Email);
                return new LoginResponse(
                    Success: false,
                    Message: "Email chưa được xác nhận. Vui lòng kiểm tra hộp thư và click vào link xác nhận.",
                    RequireEmailVerification: true
                );
            }

            // Verify password
            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User {UserId} is locked out", user.Id);
                return new LoginResponse(
                    Success: false,
                    Message: "Tài khoản tạm thời bị khóa do đăng nhập sai quá nhiều lần. Vui lòng thử lại sau."
                );
            }

            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed login attempt for {Email}", request.Email);
                return new LoginResponse(
                    Success: false,
                    Message: "Email hoặc mật khẩu không đúng"
                );
            }

            // Generate tokens
            var accessToken = _jwtTokenService.GenerateAccessToken(user);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            // Save refresh token
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(request.RememberMe ? 30 : 7);
            user.LastLoginAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);

            _logger.LogInformation("User {UserId} logged in successfully", user.Id);

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Member";

            return new LoginResponse(
                Success: true,
                Message: "Đăng nhập thành công",
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                User: new UserInfo(
                    UserId: user.Id.ToString(),
                    Email: user.Email!,
                    FullName: user.FullName ?? user.Email!,
                    SubscriptionTier: user.SubscriptionTier.ToString(),
                    IsPremium: user.IsPremium(),
                    IsEmailVerified: user.IsEmailVerified,
                    Role: role
                )
            );
        }
    }
}

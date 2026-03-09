using ADHDChecklist.API.Data;
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
        private readonly AppDbContext _context;
        private readonly ILogger<LoginCommandHandler> _logger;

        public LoginCommandHandler(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtTokenService jwtTokenService,
            AppDbContext context,
            ILogger<LoginCommandHandler> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtTokenService = jwtTokenService;
            _context = context;
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

            // 5. SESSION CAPPING: Root-Cause Fix #2
            // Root-Cause Fix #1: Explicitly load tokens for accurate capping
            await _context.Entry(user).Collection(u => u.RefreshTokens).LoadAsync(cancellationToken);

            // Limit to 10 active sessions. Revoke oldest if exceeded.
            var activeTokens = user.RefreshTokens.Where(rt => rt.RevokedAt == null && !rt.IsExpired).OrderBy(rt => rt.CreatedAt).ToList();
            if (activeTokens.Count >= 10)
            {
                var tokensToRevoke = activeTokens.Take(activeTokens.Count - 9);
                foreach (var t in tokensToRevoke)
                {
                    t.RevokedAt = DateTime.UtcNow;
                }
            }

            // Generate tokens
            var accessToken = await _jwtTokenService.GenerateAccessToken(user);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            var refreshTokenHash = _jwtTokenService.HashToken(refreshToken);

            // Save refresh token (Multi-device support)
            user.RefreshTokens.Add(new Entities.Common.RefreshToken
            {
                TokenHash = refreshTokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(request.RememberMe ? 30 : 7),
                CreatedByIp = "N/A" // Optional: track IP if needed, but keeping it simple for now
            });
            
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

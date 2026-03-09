using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities.Common;
using ADHDChecklist.API.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Google.Apis.Auth;
namespace ADHDChecklist.API.Features.Auth.GoogleLogin
{
    // ============================================
    // HANDLER
    // ============================================
    public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, GoogleLoginResponse>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        private readonly ILogger<GoogleLoginCommandHandler> _logger;

        public GoogleLoginCommandHandler(
            UserManager<ApplicationUser> userManager,
            IJwtTokenService jwtTokenService,
            IConfiguration configuration,
            AppDbContext context,
            ILogger<GoogleLoginCommandHandler> logger)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _configuration = configuration;
            _context = context;
            _logger = logger;
        }

        public async Task<GoogleLoginResponse> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Verify Google token
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _configuration["Authentication:Google:ClientId"]! }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(request.GoogleIdToken, settings);

                if (payload == null)
                {
                    return new GoogleLoginResponse(
                        Success: false,
                        Message: "Google token không hợp lệ"
                    );
                }

                // 1. Find user by Google Login (Standard Identity approach)
                var user = await _userManager.FindByLoginAsync("Google", payload.Subject);
                bool isNewUser = false;

                if (user == null)
                {
                    // 2. Fallback: Search by Email
                    user = await _userManager.FindByEmailAsync(payload.Email);

                    if (user != null)
                    {
                        // 3. SECURITY GUARD: Root-Cause #3
                        // If account exists but email is not verified, do not auto-link!
                        if (!user.IsEmailVerified)
                        {
                            _logger.LogWarning("Security conflict: User {Email} exists but is not verified. Blocked Google Link.", payload.Email);
                            return new GoogleLoginResponse(
                                Success: false,
                                Message: "Tài khoản của bạn đã tồn tại nhưng chưa được xác nhận qua email. Vui lòng đăng nhập bằng mật khẩu để liên kết tài khoản Google này."
                            );
                        }

                        // Link existing verified account to Google
                        await _userManager.AddLoginAsync(user, new UserLoginInfo("Google", payload.Subject, "Google"));
                        _logger.LogInformation("Linked existing verified account {Email} to Google", payload.Email);
                    }
                    else
                    {
                        // 4. Create new user
                        user = new ApplicationUser
                        {
                            UserName = payload.Email,
                            Email = payload.Email,
                            FullName = payload.Name,
                            EmailConfirmed = true,
                            IsEmailVerified = true, // Trusted from Google
                            GoogleId = payload.Subject,
                            GoogleProfilePicture = payload.Picture,
                            SubscriptionTier = SubscriptionTier.Free,
                            CreatedAt = DateTime.UtcNow,
                            TimeZone = "Asia/Ho_Chi_Minh"
                        };

                        var createResult = await _userManager.CreateAsync(user);
                        if (!createResult.Succeeded)
                        {
                            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                            _logger.LogError("Failed to create user from Google: {Errors}", errors);
                            return new GoogleLoginResponse(false, "Không thể tạo tài khoản từ Google.");
                        }

                        await _userManager.AddLoginAsync(user, new UserLoginInfo("Google", payload.Subject, "Google"));
                        isNewUser = true;
                    }
                }

                // Check if account is active
                if (!user.IsActive)
                {
                    return new GoogleLoginResponse(
                        Success: false,
                        Message: "Tài khoản đã bị khóa"
                    );
                }

                // Root-Cause Fix #1: Ensure RefreshTokens collection is loaded for accurate capping
                await _context.Entry(user).Collection(u => u.RefreshTokens).LoadAsync(cancellationToken);

                // SESSION CAPPING: Root-Cause Fix #2
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

                // Generate tokens (Using the async service)
                var accessToken = await _jwtTokenService.GenerateAccessToken(user);
                var refreshToken = _jwtTokenService.GenerateRefreshToken();
                var refreshTokenHash = _jwtTokenService.HashToken(refreshToken);

                // Save refresh token (Multi-device support)
                user.RefreshTokens.Add(new Entities.Common.RefreshToken
                {
                    TokenHash = refreshTokenHash,
                    ExpiresAt = DateTime.UtcNow.AddDays(30),
                    CreatedByIp = "N/A"
                });
                
                user.LastLoginAt = DateTime.UtcNow;

                // Update info if changed
                user.GoogleProfilePicture = payload.Picture;

                await _userManager.UpdateAsync(user);

                _logger.LogInformation("User {UserId} logged in via Google", user.Id);

                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "Member";

                return new GoogleLoginResponse(
                    Success: true,
                    Message: isNewUser ? "Chào mừng bạn đến với Bloomento!" : "Đăng nhập thành công!",
                    AccessToken: accessToken,
                    RefreshToken: refreshToken,
                    User: new UserInfoGg(
                        UserId: user.Id.ToString(),
                        Email: user.Email!,
                        FullName: user.FullName ?? user.Email!,
                        SubscriptionTier: user.SubscriptionTier.ToString(),
                        IsPremium: user.IsPremium(),
                        Role: role
                    ),
                    IsNewUser: isNewUser
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google login fatal error");
                return new GoogleLoginResponse(false, "Có lỗi xảy ra trong quá trình xác thực Google.");
            }
        }
    }
}

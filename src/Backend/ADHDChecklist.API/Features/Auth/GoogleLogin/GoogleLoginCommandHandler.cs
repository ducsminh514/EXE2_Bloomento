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
        private readonly ILogger<GoogleLoginCommandHandler> _logger;

        public GoogleLoginCommandHandler(
            UserManager<ApplicationUser> userManager,
            IJwtTokenService jwtTokenService,
            IConfiguration configuration,
            ILogger<GoogleLoginCommandHandler> logger)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _configuration = configuration;
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

                // Find or create user
                var user = await _userManager.FindByEmailAsync(payload.Email);
                bool isNewUser = false;

                if (user == null)
                {
                    // Create new user from Google account
                    user = new ApplicationUser
                    {
                        UserName = payload.Email,
                        Email = payload.Email,
                        FullName = payload.Name,
                        EmailConfirmed = true,
                        IsEmailVerified = true, // Google verified
                        GoogleId = payload.Subject,
                        GoogleProfilePicture = payload.Picture,
                        SubscriptionTier = SubscriptionTier.Free,
                        CreatedAt = DateTime.UtcNow,
                        TimeZone = "Asia/Ho_Chi_Minh"
                    };

                    var result = await _userManager.CreateAsync(user);

                    if (!result.Succeeded)
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        _logger.LogError("Failed to create user from Google login: {Errors}", errors);
                        return new GoogleLoginResponse(
                            Success: false,
                            Message: "Không thể tạo tài khoản. Vui lòng thử lại."
                        );
                    }

                    isNewUser = true;
                    _logger.LogInformation("New user created from Google login: {UserId}", user.Id);
                }
                else
                {
                    // Update Google info if changed
                    if (user.GoogleId != payload.Subject)
                    {
                        user.GoogleId = payload.Subject;
                        user.GoogleProfilePicture = payload.Picture;
                        await _userManager.UpdateAsync(user);
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

                // Generate tokens
                var accessToken = _jwtTokenService.GenerateAccessToken(user);
                var refreshToken = _jwtTokenService.GenerateRefreshToken();

                // Save refresh token
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(30);
                user.LastLoginAt = DateTime.UtcNow;

                await _userManager.UpdateAsync(user);

                _logger.LogInformation("User {UserId} logged in via Google", user.Id);

                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "Member";

                return new GoogleLoginResponse(
                    Success: true,
                    Message: isNewUser ? "Đăng ký thành công qua Google!" : "Đăng nhập thành công!",
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
                _logger.LogError(ex, "Google login failed");
                return new GoogleLoginResponse(
                    Success: false,
                    Message: "Đăng nhập Google thất bại. Vui lòng thử lại."
                );
            }
        }
    }
}

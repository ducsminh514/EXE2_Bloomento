using ADHDChecklist.API.Entities.Common;
using ADHDChecklist.API.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace ADHDChecklist.API.Features.Auth.RefreshToken
{
    // ============================================
    // HANDLER
    // ============================================
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ILogger<RefreshTokenCommandHandler> _logger;

        public RefreshTokenCommandHandler(
            UserManager<ApplicationUser> userManager,
            IJwtTokenService jwtTokenService,
            ILogger<RefreshTokenCommandHandler> logger)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _logger = logger;
        }

        public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            // Find user by refresh token
            var users = _userManager.Users.Where(u => u.RefreshToken == request.RefreshToken);
            var user = users.FirstOrDefault();

            if (user == null)
            {
                _logger.LogWarning("Refresh token not found");
                return new RefreshTokenResponse(
                    Success: false,
                    Message: "Token không hợp lệ"
                );
            }

            // Validate refresh token
            if (!user.IsRefreshTokenValid())
            {
                _logger.LogWarning("Expired refresh token for user {UserId}", user.Id);
                return new RefreshTokenResponse(
                    Success: false,
                    Message: "Token đã hết hạn. Vui lòng đăng nhập lại."
                );
            }

            // Check if account is active
            if (!user.IsActive)
            {
                return new RefreshTokenResponse(
                    Success: false,
                    Message: "Tài khoản đã bị khóa"
                );
            }

            // Generate new tokens
            var newAccessToken = _jwtTokenService.GenerateAccessToken(user);
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

            // Update refresh token
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Tokens refreshed for user {UserId}", user.Id);

            return new RefreshTokenResponse(
                Success: true,
                Message: "Token đã được làm mới",
                AccessToken: newAccessToken,
                RefreshToken: newRefreshToken
            );
        }
    }
}

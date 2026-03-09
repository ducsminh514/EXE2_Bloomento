using ADHDChecklist.API.Data;
using ADHDChecklist.API.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Features.Auth.Logout
{
    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, LogoutResponse>
    {
        private readonly AppDbContext _context;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ILogger<LogoutCommandHandler> _logger;

        public LogoutCommandHandler(AppDbContext context, IJwtTokenService jwtTokenService, ILogger<LogoutCommandHandler> logger)
        {
            _context = context;
            _jwtTokenService = jwtTokenService;
            _logger = logger;
        }

        public async Task<LogoutResponse> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                return new LogoutResponse(true, "Đã đăng xuất thành công (Token trống)");
            }

            var tokenHash = _jwtTokenService.HashToken(request.RefreshToken);
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

            if (refreshToken != null)
            {
                refreshToken.RevokedAt = DateTime.UtcNow;
                refreshToken.GracePeriodExpiresAt = null; // Kill immediately on explicit logout
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Refresh token revoked for user {UserId} via logout.", refreshToken.UserId);
            }

            return new LogoutResponse(true, "Đã đăng xuất thành công");
        }
    }
}

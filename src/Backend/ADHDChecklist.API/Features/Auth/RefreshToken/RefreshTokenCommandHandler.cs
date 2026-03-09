using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities.Common;
using ADHDChecklist.API.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Features.Auth.RefreshToken
{
    // ============================================
    // HANDLER
    // ============================================
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly AppDbContext _context;
        private readonly ILogger<RefreshTokenCommandHandler> _logger;

        public RefreshTokenCommandHandler(
            UserManager<ApplicationUser> userManager,
            IJwtTokenService jwtTokenService,
            AppDbContext context,
            ILogger<RefreshTokenCommandHandler> logger)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _context = context;
            _logger = logger;
        }

        public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var tokenHash = _jwtTokenService.HashToken(request.RefreshToken);

            // Find token and include user
            var refreshToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

            if (refreshToken == null)
            {
                _logger.LogWarning("Refresh token not found or invalid hash.");
                return new RefreshTokenResponse(false, "Token không hợp lệ");
            }

            var user = refreshToken.User;

            // 1. REUSE DETECTION (MALICIOUS OR EXPIRED GRACE)
            if (refreshToken.IsRevoked)
            {
                // If within grace period, we allow it (Cross-tab support)
                if (refreshToken.GracePeriodExpiresAt != null && DateTime.UtcNow <= refreshToken.GracePeriodExpiresAt)
                {
                    _logger.LogInformation("Refresh within grace period for user {UserId}. Returning existing replacement token.", user.Id);
                    
                    // Root-Cause Fix #3: Instead of generating a new chain, find the token that replaced this one
                    var replacementToken = await _context.RefreshTokens
                        .FirstOrDefaultAsync(rt => rt.TokenHash == refreshToken.ReplacedByTokenHash, cancellationToken);

                    if (replacementToken != null && replacementToken.IsActive)
                    {
                        // We can't return the raw string because we only have the hash.
                        // However, the client should ALREADY have the new token if they hit this grace period.
                        // But to be robust, we MUST handle the case where they lost it.
                        // Since we hash everything, we can't recover the original string.
                        // Decision: In grace period, if they hit this, we return a special status or 
                        // just let them keep the one they have. 
                        // Actually, the most secure way is to return the SAME response they would have gotten.
                        // But we don't store the raw token string (and shouldn't).
                        // FIX: If in grace, return failure with a "Retry with existing" message or just 
                        // let the next request (which should have the new token) succeed.
                        return new RefreshTokenResponse(false, "Token đang được làm mới. Vui lòng thử lại với token mới nhất.");
                    }
                }

                // If the grace period has expired, this is a potential hijacking attempt
                _logger.LogCritical("TOKEN REUSE DETECTED! Revoking all tokens for user {UserId}", user.Id);
                
                var activeTokens = _context.RefreshTokens.Where(rt => rt.UserId == user.Id && rt.RevokedAt == null);
                foreach (var t in activeTokens)
                {
                    t.RevokedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync(cancellationToken);

                return new RefreshTokenResponse(false, "Cảnh báo bảo mật: Phiên làm việc đã bị vô hiệu hóa.");
            }

            // 2. EXPIRY CHECK
            if (refreshToken.IsExpired)
            {
                return new RefreshTokenResponse(false, "Token đã hết hạn. Vui lòng đăng nhập lại.");
            }

            // 3. ACCOUNT STATUS
            if (!user.IsActive)
            {
                return new RefreshTokenResponse(false, "Tài khoản đã bị khóa");
            }

            // 4. ROTATE TOKEN
            var newAccessToken = await _jwtTokenService.GenerateAccessToken(user);
            var newRefreshTokenString = _jwtTokenService.GenerateRefreshToken();
            var newRefreshTokenHash = _jwtTokenService.HashToken(newRefreshTokenString);

            // Mark old token as revoked but start grace period
            if (!refreshToken.IsRevoked)
            {
                refreshToken.RevokedAt = DateTime.UtcNow;
                refreshToken.GracePeriodExpiresAt = DateTime.UtcNow.AddSeconds(60); 
                refreshToken.ReplacedByTokenHash = newRefreshTokenHash;
                refreshToken.ConcurrencyStamp = Guid.NewGuid(); // Rotate stamp
            }

            // 5. SESSION CAPPING (Max 10 active tokens per user)
            var activeSessionCount = await _context.RefreshTokens
                .CountAsync(rt => rt.UserId == user.Id && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow, cancellationToken);
            
            if (activeSessionCount >= 10)
            {
                var oldestSessions = await _context.RefreshTokens
                    .Where(rt => rt.UserId == user.Id && rt.RevokedAt == null)
                    .OrderBy(rt => rt.CreatedAt)
                    .Take(activeSessionCount - 9)
                    .ToListAsync(cancellationToken);
                
                foreach (var s in oldestSessions)
                {
                    s.RevokedAt = DateTime.UtcNow;
                    s.GracePeriodExpiresAt = null;
                }
            }

            // Add new token
            _context.RefreshTokens.Add(new Entities.Common.RefreshToken
            {
                UserId = user.Id,
                TokenHash = newRefreshTokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedByIp = "N/A"
            });

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning("Concurrency conflict detected during token rotation for user {UserId}", user.Id);
                return new RefreshTokenResponse(false, "Vui lòng thử lại (concurrency conflict)");
            }

            _logger.LogInformation("Tokens rotated for user {UserId}", user.Id);

            return new RefreshTokenResponse(
                Success: true,
                Message: "Token đã được làm mới",
                AccessToken: newAccessToken,
                RefreshToken: newRefreshTokenString
            );
        }
    }
}

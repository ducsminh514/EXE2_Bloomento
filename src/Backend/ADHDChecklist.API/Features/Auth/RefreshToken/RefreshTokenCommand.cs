using MediatR;

namespace ADHDChecklist.API.Features.Auth.RefreshToken
{
    // ============================================
    // REQUEST & RESPONSE
    // ============================================
    public record RefreshTokenCommand(
        string RefreshToken
    ) : IRequest<RefreshTokenResponse>;

    public record RefreshTokenResponse(
        bool Success,
        string Message,
        string? AccessToken = null,
        string? RefreshToken = null
    );
}

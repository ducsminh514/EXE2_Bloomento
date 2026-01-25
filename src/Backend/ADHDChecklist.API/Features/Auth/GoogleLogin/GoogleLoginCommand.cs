using MediatR;

namespace ADHDChecklist.API.Features.Auth.GoogleLogin
{
    // ============================================
    // REQUEST & RESPONSE
    // ============================================
    public record GoogleLoginCommand(
        string GoogleIdToken
    ) : IRequest<GoogleLoginResponse>;

    public record GoogleLoginResponse(
        bool Success,
        string Message,
        string? AccessToken = null,
        string? RefreshToken = null,
        UserInfoGg? User = null,
        bool IsNewUser = false
    );

    public record UserInfoGg(
        string UserId,
        string Email,
        string FullName,
        string SubscriptionTier,
        bool IsPremium
    );
}

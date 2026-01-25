using MediatR;

namespace ADHDChecklist.API.Features.Auth.VerifyEmail
{
// ============================================
// REQUEST & RESPONSE
// ============================================
public record VerifyEmailCommand(
    string UserId,
    string Token
) : IRequest<VerifyEmailResponse>;

    public record VerifyEmailResponse(
        bool Success,
        string Message
    );
}

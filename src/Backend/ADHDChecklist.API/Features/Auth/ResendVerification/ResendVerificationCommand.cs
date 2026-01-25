using MediatR;

namespace ADHDChecklist.API.Features.Auth.ResendVerification
{
    public record ResendVerificationCommand(
      string Email
  ) : IRequest<ResendVerificationResponse>;

    public record ResendVerificationResponse(
        bool Success,
        string Message
    );
}

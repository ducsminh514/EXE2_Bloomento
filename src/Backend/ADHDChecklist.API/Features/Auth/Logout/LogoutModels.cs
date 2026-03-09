using MediatR;

namespace ADHDChecklist.API.Features.Auth.Logout
{
    public record LogoutCommand(string RefreshToken) : IRequest<LogoutResponse>;
    public record LogoutResponse(bool Success, string Message);
}

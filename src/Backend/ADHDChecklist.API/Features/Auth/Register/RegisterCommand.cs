using MediatR;

namespace ADHDChecklist.API.Features.Auth.Register
{
    public record RegisterCommand(
        string Email,
        string Password,
        string FullName
    ) : IRequest<RegisterResponse>;


}

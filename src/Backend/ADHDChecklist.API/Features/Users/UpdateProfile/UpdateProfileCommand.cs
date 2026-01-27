using MediatR;

namespace ADHDChecklist.API.Features.Auth;

public record UpdateProfileCommand(
    string UserId,
    string FullName
) : IRequest<UpdateProfileResponse>;

public record UpdateProfileResponse(
    bool Success,
    string Message,
    string? NewFullName = null
);

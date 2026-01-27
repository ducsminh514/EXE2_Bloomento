using MediatR;
using ADHDChecklist.API.Features.Auth; // For Command/Response
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ADHDChecklist.API.Features.Users.UpdateProfile
{
    public class UpdateProfileRequest
    {
        public string FullName { get; set; } = string.Empty;
    }

    public static class UpdateProfileEndpoint
    {
        public static void MapUpdateProfile(this IEndpointRouteBuilder app)
        {
            app.MapPut("/api/users/profile", async (
                [FromBody] UpdateProfileRequest request,
                HttpContext httpContext,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                var command = new UpdateProfileCommand(userId, request.FullName);
                var result = await mediator.Send(command, ct);

                return result.Success
                    ? Results.Ok(result)
                    : Results.BadRequest(result);
            })
            .WithTags("Users")
            .WithName("UpdateProfile")
            .RequireAuthorization();
        }
    }
}

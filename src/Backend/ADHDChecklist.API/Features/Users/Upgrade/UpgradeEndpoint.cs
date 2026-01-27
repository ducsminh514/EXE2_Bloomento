using MediatR;
using ADHDChecklist.API.Entities.Common;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ADHDChecklist.API.Features.Users.Upgrade;

// DTO for Request Body
public record UpgradeRequest(SubscriptionTier Tier); 

public static class UpgradeEndpoint
{
    public static void MapUpgrade(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users/upgrade", async (
            [FromBody] UpgradeRequest request,
            HttpContext httpContext,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var command = new UpgradeCommand(userId, request.Tier);
            var result = await mediator.Send(command, ct);

            return result.Success
                ? Results.Ok(result)
                : Results.BadRequest(result);
        })
        .WithTags("Users")
        .WithName("UpgradeAccount")
        .RequireAuthorization();
    }
}

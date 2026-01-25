using System.Security.Claims;
using MediatR;

namespace ADHDChecklist.API.Features.Tasks.ToggleTask
{
    public static class ToggleTaskCompletionEndpoint
    {
        public static void MapToggleTaskCompletion(this IEndpointRouteBuilder app)
        {
            app.MapPatch("/api/tasks/{id:guid}/toggle-completion", async (
                Guid id,
                ClaimsPrincipal user,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var command = new ToggleTaskCompletionCommand(id, userId);
                var result = await mediator.Send(command, ct);

                return result
                    ? Results.Ok(new { success = true })
                    : Results.NotFound();
            })
            .RequireAuthorization()
            .WithTags("Tasks")
            .WithName("ToggleTaskCompletion")
            .Produces(200)
            .Produces(404);
        }
    }
}

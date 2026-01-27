using System.Security.Claims;
using MediatR;

namespace ADHDChecklist.API.Features.Tasks.MoveTask
{
    public static class MoveTaskToInboxEndpoint
    {
        public static void MapMoveTaskToInbox(this IEndpointRouteBuilder app)
        {
            app.MapPatch("/api/tasks/{id:guid}/move-to-inbox", async (
                Guid id,
                ClaimsPrincipal user,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var command = new MoveTaskToInboxCommand(id, userId);
                var result = await mediator.Send(command, ct);
                return result ? Results.Ok() : Results.NotFound();
            })
            .RequireAuthorization()
            .WithTags("Tasks")
            .WithName("MoveTaskToInbox");
        }
    }
}

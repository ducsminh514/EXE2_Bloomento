using System.Security.Claims;
using MediatR;

namespace ADHDChecklist.API.Features.Tasks.DeleteTask
{
    public static class DeleteTaskEndpoint
    {
        public static void MapDeleteTask(this IEndpointRouteBuilder app)
        {
            app.MapDelete("/api/tasks/{id:guid}", async (
                Guid id,
                ClaimsPrincipal user,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var command = new DeleteTaskCommand(id, userId);
                var result = await mediator.Send(command, ct);

                return result
                    ? Results.NoContent()
                    : Results.NotFound();
            })
            .RequireAuthorization()
            .WithTags("Tasks")
            .WithName("DeleteTask")
            .Produces(204)
            .Produces(404);
        }
    }
}

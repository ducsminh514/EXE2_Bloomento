using System.Security.Claims;
using MediatR;

namespace ADHDChecklist.API.Features.Tasks.MoveTask
{
    public static class MoveTaskToTimeSlotEndpoint
    {
        public static void MapMoveTaskToTimeSlot(this IEndpointRouteBuilder app)
        {
            app.MapPatch("/api/tasks/{id:guid}/move-to-timeslot", async (
                Guid id,
                MoveToTimeSlotRequest request,
                ClaimsPrincipal user,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var command = new MoveTaskToTimeSlotCommand(
                    id,
                    TimeOnly.Parse(request.TimeBlockStart),
                    TimeOnly.Parse(request.TimeBlockEnd),
                    userId
                );

                var result = await mediator.Send(command, ct);
                return result ? Results.Ok() : Results.NotFound();
            })
            .RequireAuthorization()
            .WithTags("Tasks")
            .WithName("MoveTaskToTimeSlot");
        }
    }
}

using System.Security.Claims;
using ADHDChecklist.API.Shared.DTOs;
using MediatR;

namespace ADHDChecklist.API.Features.Tasks.UpdateTask
{
    public static class UpdateTaskEndpoint
    {
        public static void MapUpdateTask(this IEndpointRouteBuilder app)
        {
            app.MapPut("/api/tasks/{id:guid}", async (
                Guid id,
                UpdateTaskRequest request,
                ClaimsPrincipal user,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var command = new UpdateTaskCommand(
                    id,
                    request.Title,
                    request.Description,
                    request.CategoryId,
                    request.ScheduledDate,
                    request.TimeBlockStart,
                    request.TimeBlockEnd,
                    request.Duration,
                    request.Priority,
                    request.IsCompleted,
                    userId
                );

                var result = await mediator.Send(command, ct);

                return result != null
                    ? Results.Ok(result)
                    : Results.NotFound(new { message = "Task không tồn tại" });
            })
            .RequireAuthorization()
            .WithTags("Tasks")
            .WithName("UpdateTask")
            .Produces<TaskResponse>(200)
            .Produces(404);
        }
    }
}

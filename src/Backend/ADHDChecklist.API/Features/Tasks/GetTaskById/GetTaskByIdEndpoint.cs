using System.Security.Claims;
using MediatR;
using ADHDChecklist.API.Shared.DTOs;
namespace ADHDChecklist.API.Features.Tasks.GetTaskById
{
    public static class GetTaskByIdEndpoint
    {
        public static void MapGetTaskById(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/tasks/{id:guid}", async (
                Guid id,
                ClaimsPrincipal user,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var query = new GetTaskByIdQuery(id, userId);
                var result = await mediator.Send(query, ct);

                return result != null
                    ? Results.Ok(result)
                    : Results.NotFound(new { message = "Task không tồn tại" });
            })
            .RequireAuthorization()
            .WithTags("Tasks")
            .WithName("GetTaskById")
            .Produces<TaskResponse>(200)
            .Produces(404);
        }
    }
}

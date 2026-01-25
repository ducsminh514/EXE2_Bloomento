using System.Security.Claims;
using ADHDChecklist.API.Shared.DTOs;
using MediatR;

namespace ADHDChecklist.API.Features.Tasks.GetTasksByDate
{
    public static class GetTasksByDateEndpoint
    {
        public static void MapGetTasksByDate(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/tasks", async (
                DateOnly? date,
                ClaimsPrincipal user,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

                var query = new GetTasksByDateQuery(targetDate, userId);
                var result = await mediator.Send(query, ct);

                return Results.Ok(result);
            })
            .RequireAuthorization()
            .WithTags("Tasks")
            .WithName("GetTasksByDate")
            .Produces<TaskListResponse>(200);
        }
    }
}

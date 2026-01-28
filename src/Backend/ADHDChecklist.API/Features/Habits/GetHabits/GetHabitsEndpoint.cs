using System.Security.Claims;
using ADHDChecklist.API.Shared.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ADHDChecklist.API.Features.Habits.GetHabits
{
    public static class GetHabitsEndpoint
    {
        public static void MapGetHabits(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/habits", async (
                ClaimsPrincipal user,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
                
                var query = new GetHabitsQuery(userId);
                var result = await mediator.Send(query, ct);

                return Results.Ok(result);
            })
            .RequireAuthorization()
            .WithTags("Habits")
            .WithName("GetHabits")
            .Produces<List<HabitResponse>>(200);
        }
    }
}

using System.Security.Claims;
using MediatR;

namespace ADHDChecklist.API.Features.Analytics.FreeTier
{
    public static class AnalyticsEndpoints
    {
        public static void MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/analytics/weekly", async (
                ClaimsPrincipal user,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var query = new GetWeeklyAnalyticsQuery(userId);
                var result = await mediator.Send(query, ct);
                return Results.Ok(result);
            })
            .RequireAuthorization()
            .WithTags("Analytics")
            .WithName("GetWeeklyAnalytics")
            .WithDescription("FREE tier: Simple weekly analytics");
        }
    }
}

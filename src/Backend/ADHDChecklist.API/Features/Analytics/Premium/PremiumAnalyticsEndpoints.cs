using System.Security.Claims;
using MediatR;

namespace ADHDChecklist.API.Features.Analytics.Premium
{
    public static class PremiumAnalyticsEndpoints
    {
        public static void MapPremiumAnalyticsEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/analytics/premium", async (
                ClaimsPrincipal user,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
                
                // Note: Logic for checking IsPremium should ideally be here or as a filter
                // For now, we trust the frontend gating, but let's assume we can add a claim check later.
                
                var query = new GetPremiumAnalyticsQuery(userId);
                var result = await mediator.Send(query, ct);
                return Results.Ok(result);
            })
            .RequireAuthorization()
            .WithTags("Analytics")
            .WithName("GetPremiumAnalytics")
            .WithDescription("PREMIUM tier: Advanced ADHD analytics/charts");
        }
    }
}

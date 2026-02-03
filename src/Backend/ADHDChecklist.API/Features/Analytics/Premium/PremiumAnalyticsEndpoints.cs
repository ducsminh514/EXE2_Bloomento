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
                var tierClaim = user.FindFirst("SubscriptionTier")?.Value;
                var isPremiumClaim = user.FindFirst("IsPremium")?.Value;
                var subClaim = user.FindFirst("subscription")?.Value;

                // Restrict to Premium (1) and Family (2) or explicit Premium flags
                bool isAuthorized = tierClaim == "1" || tierClaim == "2" || 
                                   (isPremiumClaim != null && isPremiumClaim.Equals("true", StringComparison.OrdinalIgnoreCase)) ||
                                   subClaim == "Premium";

                if (!isAuthorized)
                {
                    return Results.Problem(
                        detail: "This feature is available for Premium and Family plans only.",
                        statusCode: 403,
                        title: "Premium Feature");
                }
                
                var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim)) return Results.Unauthorized();

                var userId = Guid.Parse(userIdClaim);
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

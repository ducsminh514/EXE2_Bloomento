using ADHDChecklist.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace ADHDChecklist.API.Features.AI.BreakdownTask
{
    public static class BreakdownTaskEndpoint
    {
        public static void MapBreakdownTask(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/ai/breakdown", async (
                [FromBody] BreakdownTaskRequest request,
                IGeminiService geminiService,
                System.Security.Claims.ClaimsPrincipal user) =>
            {
                if (string.IsNullOrWhiteSpace(request.TaskTitle))
                {
                    return Results.BadRequest("Task title is required");
                }

                // Check functionality restriction
                var tierClaim = user.FindFirst("SubscriptionTier")?.Value;
                // Tier is stored as int in JWT (0 = Free, 1 = Premium, 2 = Family) or string "Free" if legacy
                if (string.IsNullOrEmpty(tierClaim) || tierClaim == "Free" || tierClaim == "0")
                {
                     return Results.Problem(
                        detail: "This feature is available for Premium and Family plans only.",
                        statusCode: 403,
                        title: "Premium Feature");
                }

                try
                {
                    var steps = await geminiService.BreakDownTaskAsync(request.TaskTitle);
                    return Results.Ok(new BreakdownTaskResponse(steps));
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithTags("AI")
            .RequireAuthorization()
            .RequireRateLimiting("GeminiPolicy");
        }
    }

    public record BreakdownTaskRequest(string TaskTitle);
    public record BreakdownTaskResponse(string[] Steps);
}

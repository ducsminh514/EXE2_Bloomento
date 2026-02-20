using System.Security.Claims;
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
                ADHDChecklist.API.Data.AppDbContext dbContext,
                System.Security.Claims.ClaimsPrincipal user) =>
            {
                if (string.IsNullOrWhiteSpace(request.TaskTitle))
                {
                    return Results.BadRequest("Task title is required");
                }

                // Check functionality restriction
                var userId = Guid.Parse(user.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)!);
                var validUser = await dbContext.Users.FindAsync(new object[] { userId });

                if (validUser == null) return Results.Unauthorized();

                var tier = validUser.SubscriptionTier;
                var isFree = tier == ADHDChecklist.API.Entities.Common.SubscriptionTier.Free;

                if (isFree)
                {
                    if (validUser.LifetimeAiUsageCount >= 3)
                    {
                         return Results.Problem(
                            detail: "Gói miễn phí chỉ được dùng AI 3 lần. Vui lòng nâng cấp để sử dụng không giới hạn!",
                            statusCode: 403,
                            title: "Hết lượt dùng thử miễn phí");
                    }
                }

                try
                {
                    var steps = await geminiService.BreakDownTaskAsync(request.TaskTitle);
                    
                    // Increment usage for Free tier
                    if (isFree)
                    {
                        validUser.LifetimeAiUsageCount++;
                        await dbContext.SaveChangesAsync();
                    }

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

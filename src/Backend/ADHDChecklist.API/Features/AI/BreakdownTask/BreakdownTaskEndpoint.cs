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
                IGeminiService geminiService) =>
            {
                if (string.IsNullOrWhiteSpace(request.TaskTitle))
                {
                    return Results.BadRequest("Task title is required");
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

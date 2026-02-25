using System.Security.Claims;
using ADHDChecklist.API.Services;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using ADHDChecklist.API.Entities.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Features.AI.BreakdownTask
{
    public static class BreakdownTaskEndpoint
    {
        public static void MapBreakdownTask(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/ai/breakdown", async (
                [FromBody] BreakdownTaskRequest request,
                IGeminiService geminiService,
                AppDbContext dbContext,
                ClaimsPrincipal user) =>
            {
                if (string.IsNullOrWhiteSpace(request.TaskTitle))
                {
                    return Results.BadRequest("Task title is required");
                }

                // Check user and tier
                var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var validUser = await dbContext.Users.FindAsync(new object[] { userId });

                if (validUser == null) return Results.Unauthorized();

                var isFree = validUser.SubscriptionTier == SubscriptionTier.Free;

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

                    // P0-1 FIX: If a TaskId is provided, create real sub-tasks in DB
                    if (request.TaskId.HasValue && steps.Length > 0)
                    {
                        // Verify parent task exists and belongs to user
                        var parentTask = await dbContext.Tasks
                            .FirstOrDefaultAsync(t => t.Id == request.TaskId.Value &&
                                                      t.UserId == userId &&
                                                      t.DeletedAt == null);

                        if (parentTask != null)
                        {
                            var subTasks = steps.Select((step, index) => new Entities.Task
                            {
                                Id = Guid.NewGuid(),
                                UserId = userId,
                                ParentTaskId = parentTask.Id,
                                Title = step,
                                ScheduledDate = parentTask.ScheduledDate,
                                Priority = parentTask.Priority,
                                DopamineType = "Low", // Sub-tasks are typically hard work
                                IsCompleted = false,
                                OrderIndex = index,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow,
                                AssignedUserId = parentTask.AssignedUserId,
                                AssignmentStatus = parentTask.AssignmentStatus,
                                FamilyId = parentTask.FamilyId,
                            }).ToList();

                            dbContext.Tasks.AddRange(subTasks);
                            await dbContext.SaveChangesAsync();

                            return Results.Ok(new BreakdownTaskResponse(
                                steps,
                                subTasks.Select(t => t.Id).ToArray()
                            ));
                        }
                    }

                    // Fallback: just return steps as strings (no TaskId provided)
                    return Results.Ok(new BreakdownTaskResponse(steps, Array.Empty<Guid>()));
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

    public record BreakdownTaskRequest(string TaskTitle, Guid? TaskId = null);
    public record BreakdownTaskResponse(string[] Steps, Guid[] SubTaskIds);
}

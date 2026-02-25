using System.Security.Claims;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ADHDChecklist.API.Features.Focus
{
    /// <summary>
    /// API để lưu FocusSession sau khi timer kết thúc.
    /// Đây là nguồn data cho Premium Analytics (Time Blindness, Energy Heatmap).
    /// </summary>
    public static class FocusSessionEndpoints
    {
        public static void MapFocusSessionEndpoints(this IEndpointRouteBuilder app)
        {
            // POST: Tạo mới FocusSession (gọi khi timer bắt đầu hoặc kết thúc)
            app.MapPost("/api/focus-sessions", async (
                SaveFocusSessionRequest request,
                ClaimsPrincipal user,
                AppDbContext dbContext,
                CancellationToken ct) =>
            {
                var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var session = new FocusSession
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TaskId = request.TaskId,
                    StartedAt = request.StartedAt,
                    EndedAt = DateTime.UtcNow,
                    PlannedDuration = request.PlannedDuration,
                    ActualDuration = request.ActualDuration,
                    WasCompleted = request.WasCompleted,
                    WhiteNoiseUsed = request.WhiteNoiseUsed,
                    CreatedAt = DateTime.UtcNow
                };

                dbContext.FocusSessions.Add(session);
                await dbContext.SaveChangesAsync(ct);

                return Results.Ok(new { session.Id });
            })
            .RequireAuthorization()
            .WithTags("Focus")
            .WithName("SaveFocusSession")
            .Produces(200);
        }
    }

    public record SaveFocusSessionRequest(
        Guid? TaskId,
        DateTime StartedAt,
        int PlannedDuration,   // Phút đã lên kế hoạch (VD: 25)
        int? ActualDuration,   // Thời gian thực tế đã tập trung (giây)
        bool WasCompleted,     // Có hoàn thành đủ không?
        string? WhiteNoiseUsed // Âm thanh nào đã bật? (VD: "rain")
    );
}

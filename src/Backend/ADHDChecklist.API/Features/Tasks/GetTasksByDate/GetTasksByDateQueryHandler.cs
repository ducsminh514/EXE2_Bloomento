using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Shared.DTOs;

namespace ADHDChecklist.API.Features.Tasks.GetTasksByDate;

// ============================================
// HANDLER
// ============================================
public class GetTasksByDateQueryHandler : IRequestHandler<GetTasksByDateQuery, TaskListResponse>
{
    private readonly AppDbContext _context;
    private readonly ILogger<GetTasksByDateQueryHandler> _logger;

    public GetTasksByDateQueryHandler(
        AppDbContext context,
        ILogger<GetTasksByDateQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TaskListResponse> Handle(GetTasksByDateQuery request, CancellationToken cancellationToken)
    {
        var tasks = await _context.Tasks
            .Where(t => t.UserId == request.UserId && t.ScheduledDate == request.Date)
            .Include(t => t.Category)
            // ✅ FIX: Chỉ load SubTasks nếu relationship được config đúng
            .OrderBy(t => t.TimeBlockStart ?? TimeOnly.MaxValue)
            .ThenBy(t => t.OrderIndex)
            .Select(t => new TaskResponse(
                t.Id,
                t.Title,
                t.Description,
                t.CategoryId,
                t.Category != null ? t.Category.Name : null,
                t.Category != null ? t.Category.ColorHex : null,
                t.ScheduledDate,
                t.TimeBlockStart,
                t.TimeBlockEnd,
                t.Duration,
                t.IsCompleted,
                t.CompletedAt,
                t.Priority ?? 1,
                t.IsRecurring ?? false,
                t.RecurrencePattern,
                t.ParentTaskId,
                new List<TaskResponse>(), // ✅ Empty list thay vì query SubTasks
                t.OrderIndex ?? 0,
                t.CreatedAt,
                t.UpdatedAt,
                t.RescheduleCount,
                t.DopamineType
            ))
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} tasks for user {UserId} on {Date}",
            tasks.Count, request.UserId, request.Date);

        return new TaskListResponse(
            tasks,
            tasks.Count,
            request.Date
        );
    }
}

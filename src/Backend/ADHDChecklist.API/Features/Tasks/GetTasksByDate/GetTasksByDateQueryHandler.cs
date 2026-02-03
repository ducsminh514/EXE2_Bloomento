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
        // 1. Check if user is in a family
        var userFamilyId = await _context.FamilyMembers
            .Where(fm => fm.UserId == request.UserId)
            .Select(fm => fm.FamilyId)
            .FirstOrDefaultAsync(cancellationToken);

        // 2. Build Query
        var query = _context.Tasks.AsQueryable();

        if (userFamilyId != Guid.Empty)
        {
            // Show my tasks OR family tasks
            // Note: Simplification - showing all family tasks. Can restrict to "IsShared" if needed.
            // For now, let's show all tasks linked to Family.
            query = query.Where(t => 
                (t.UserId == request.UserId && t.ScheduledDate == request.Date) || 
                (t.FamilyId == userFamilyId && t.ScheduledDate == request.Date));
        }
        else
        {
            // Show only my tasks
            query = query.Where(t => t.UserId == request.UserId && t.ScheduledDate == request.Date);
        }

        var tasks = await query
            .Include(t => t.Category)
            .Include(t => t.AssignedUser) // Include Assignee info
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
                new List<TaskResponse>(), 
                t.OrderIndex ?? 0,
                t.CreatedAt,
                t.UpdatedAt,
                t.RescheduleCount,
                t.DopamineType,
                t.FamilyId,
                t.AssignedUserId,
                t.AssignedUser != null ? t.AssignedUser.FullName : null,
                t.AssignedUser != null ? t.AssignedUser.GoogleProfilePicture : null,
                t.FamilyId.HasValue && t.AssignedUserId.HasValue
                    ? t.Family.Members.FirstOrDefault(m => m.UserId == t.AssignedUserId).Color 
                    : null,
                t.IsShared,
                t.AssignmentStatus,
                t.RejectionReason
            ))
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} tasks for user {UserId} on {Date} (FamilyId: {FamilyId})",
            tasks.Count, request.UserId, request.Date, userFamilyId);

        return new TaskListResponse(
            tasks,
            tasks.Count,
            request.Date
        );
    }
}

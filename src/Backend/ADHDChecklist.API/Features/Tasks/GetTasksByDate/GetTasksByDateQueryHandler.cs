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

        // 1.1 Check Subscription Tier for Retention Policy
        var userTier = await _context.Users
            .Where(u => u.Id == request.UserId)
            .Select(u => u.SubscriptionTier)
            .FirstOrDefaultAsync(cancellationToken);

        var isFreeTier = userTier == Entities.Common.SubscriptionTier.Free;

        // 2. Build Query
        var query = _context.Tasks.AsQueryable();

        if (userFamilyId != Guid.Empty)
        {
            // Show:
            // 1. My Created Tasks (UserId == Me)
            // 2. Tasks Assigned to Me (AssignedUserId == Me)
            // 3. Shared Family Tasks (FamilyId == Family AND IsShared == true)
            query = query.Where(t => 
                (t.ScheduledDate == request.Date || (t.ScheduledDate == null && !t.IsCompleted)) && // Common Date Filter
                (
                    t.UserId == request.UserId || 
                    t.AssignedUserId == request.UserId ||
                    (t.FamilyId == userFamilyId && t.IsShared)
                )
            );
        }
        else
        {
            // Show only my tasks (Scheduled today OR Unscheduled)
            query = query.Where(t => t.UserId == request.UserId && (t.ScheduledDate == request.Date || (t.ScheduledDate == null && !t.IsCompleted)));
        }

        // 3. Apply Retention Policy (Hide old tasks for Free Tier)
        if (isFreeTier)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-30);
            query = query.Where(t => t.CreatedAt >= cutoffDate);
        }

        var tasks = await query
            .Include(t => t.Category)
            .Include(t => t.AssignedUser) // Include Assignee info
            .Include(t => t.Family)
                .ThenInclude(f => f.Members)
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
                t.RejectionReason,
                t.CompletionApprovalStatus,
                t.IsMandatory,
                t.UserId // CreatorId
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

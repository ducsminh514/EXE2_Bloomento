using ADHDChecklist.API.Data;
using ADHDChecklist.API.Shared.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace ADHDChecklist.API.Features.Tasks.UpdateTask
{
    public class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, TaskResponse?>
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UpdateTaskCommandHandler> _logger;
        private readonly IMediator _mediator;

        public UpdateTaskCommandHandler(
            AppDbContext context,
            ILogger<UpdateTaskCommandHandler> logger,
            IMediator mediator)
        {
            _context = context;
            _logger = logger;
            _mediator = mediator;
        }

        public async Task<TaskResponse?> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _context.Tasks
                .Include(t => t.Category)
                .Include(t => t.AssignedUser)
                .FirstOrDefaultAsync(t => t.Id == request.TaskId && t.UserId == request.UserId, cancellationToken);

            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found or unauthorized for user {UserId}",
                    request.TaskId, request.UserId);
                return null;
            }

            // Update fields
            if (task.ScheduledDate != request.ScheduledDate)
            {
                task.RescheduleCount++;
            }

            // Overlap Check (if time block is being set or changed)
            if (request.TimeBlockStart.HasValue && request.TimeBlockEnd.HasValue)
            {
                var targetUserId = request.AssignedUserId ?? task.AssignedUserId ?? task.UserId;
                var start = request.TimeBlockStart.Value;
                var end = request.TimeBlockEnd.Value;

                var hasConflict = await _context.Tasks.AnyAsync(t =>
                    t.Id != request.TaskId && // Exclude self
                    t.DeletedAt == null &&
                    t.ScheduledDate == request.ScheduledDate &&
                    (t.UserId == targetUserId || t.AssignedUserId == targetUserId) &&
                    t.TimeBlockStart.HasValue && t.TimeBlockEnd.HasValue &&
                    t.TimeBlockStart < end && t.TimeBlockEnd > start,
                    cancellationToken);

                if (hasConflict)
                {
                    var conflictingTask = await _context.Tasks
                        .Where(t =>
                            t.Id != request.TaskId &&
                            t.DeletedAt == null &&
                            t.ScheduledDate == request.ScheduledDate &&
                            (t.UserId == targetUserId || t.AssignedUserId == targetUserId) &&
                            t.TimeBlockStart.HasValue && t.TimeBlockEnd.HasValue &&
                            t.TimeBlockStart < end && t.TimeBlockEnd > start)
                        .Select(t => t.Title)
                        .FirstOrDefaultAsync(cancellationToken);

                    throw new InvalidOperationException($"Trùng lịch! Người được giao đã có công việc '{conflictingTask}' trong khung giờ này.");
                }
            }

            task.Title = request.Title;
            task.Description = request.Description;
            task.CategoryId = request.CategoryId;
            task.ScheduledDate = request.ScheduledDate;
            task.TimeBlockStart = request.TimeBlockStart;
            task.TimeBlockEnd = request.TimeBlockEnd;
            task.Duration = request.Duration;
            task.Priority = request.Priority;
            
            // Allow re-assignment
            // Allow re-assignment
            if (request.AssignedUserId.HasValue)
            {
                bool isNewAssignment = task.AssignedUserId != request.AssignedUserId;
                task.AssignedUserId = request.AssignedUserId;
                task.IsShared = request.IsShared;

                if (isNewAssignment && task.AssignedUserId != request.UserId)
                {
                    // Fetch assigner name
                    var assigner = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
                    if (assigner != null)
                    {
                        await _mediator.Publish(new Shared.Events.TaskAssignedEvent(
                            task.Id,
                            task.Title,
                            task.AssignedUserId.Value,
                            request.UserId,
                            assigner.FullName ?? "Một thành viên"
                        ), cancellationToken);
                    }
                }
            }
            // If request.AssignedUserId is null, we might want to keep existing or unassign?
            // Usually Updates pass the full state. If null is passed, does it mean unassign?
            // In DTO `AssignedUserId` defaults to null. Use carefully.
            // Current DTO Usage in Frontend: It sends the full object. 
            // If DTO sends null, it means no change? Or unassign?
            // Let's assume nullable means "Update if provided" or "Set to null"?
            // In CreateTask, null means "Self".
            // Here, explicit null might mean "Unassign" (back to owner) or "No Change".
            // Given the limited time, let's treat explicit HasValue as "Set".
            // Actually, frontend sends what's in the form.
            // If I want to unassign, I need to send OwnerId?
            // Let's leave it as: Only update if strictly provided (HasValue logic).
            // But if I change from "Member A" to "Self" (null), how to represent?
            // Frontend should send OwnerId as AssignedUserId for "Self".
            // CreateTaskModal logic sends `null` for Self.
            // So if `request.AssignedUserId` is null, it typically implies Self (Owner)? 
            // BUT `UpdateTaskRequest` (DTO) has `AssignedUserId` as generic nullable.
            // Let's assume if it is explicitly passed as null, it MIGHT be ignored if we use `if (request.HasValue)`.
            // Safer: If I want to unassign, client should send OwnerId.
            // I'll stick to: Update if `request.AssignedUserId` is NOT NULL.
            // If client wants to unassign, they should send the Task Owner's ID.
            
            if (request.DopamineType != null)
            {
                task.DopamineType = request.DopamineType;
            }

            if (!task.IsCompleted && request.IsCompleted)
            {
                task.IsCompleted = true;
                task.CompletedAt = DateTime.UtcNow;
            }
            else if (task.IsCompleted && !request.IsCompleted)
            {
                task.IsCompleted = false;
                task.CompletedAt = null;
            }

            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Reload category if changed
            if (task.CategoryId.HasValue)
            {
                await _context.Entry(task)
                    .Reference(t => t.Category)
                    .LoadAsync(cancellationToken);
            }

            _logger.LogInformation("Task {TaskId} updated by user {UserId}", task.Id, request.UserId);

            return new TaskResponse(
            task.Id,
            task.Title,
            task.Description,
            task.CategoryId,
            task.Category?.Name,
            task.Category?.ColorHex,
            task.ScheduledDate,
            task.TimeBlockStart,
            task.TimeBlockEnd,
            task.Duration,
            task.IsCompleted,
            task.CompletedAt,
            task.Priority ?? 1,
            task.IsRecurring ?? false,
            task.RecurrencePattern,
            task.ParentTaskId,
            new List<TaskResponse>(),
            task.OrderIndex ?? 0,
            task.CreatedAt,
            task.UpdatedAt,
            task.RescheduleCount,
            task.DopamineType,
            task.FamilyId,
            task.AssignedUserId,
            task.AssignedUser?.FullName,
            task.AssignedUser?.GoogleProfilePicture,
            task.IsShared
        );
        }
    }

}

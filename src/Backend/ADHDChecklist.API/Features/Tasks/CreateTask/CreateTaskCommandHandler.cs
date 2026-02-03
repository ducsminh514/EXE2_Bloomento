using System.Security.Claims;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Shared.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskEntity = ADHDChecklist.API.Entities.Task;

namespace ADHDChecklist.API.Features.Tasks.CreateTask
{
    public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, TaskResponse>
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CreateTaskCommandHandler> _logger;
        private readonly IMediator _mediator;

        public CreateTaskCommandHandler(
            AppDbContext context,
            ILogger<CreateTaskCommandHandler> logger,
            IMediator mediator)
        {
            _context = context;
            _logger = logger;
            _mediator = mediator;
        }

        public async Task<TaskResponse> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
        {
            // 1. Check User Tier
            var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
            if (user == null) throw new UnauthorizedAccessException("User not found");

            if (!user.IsPremium())
            {
                // 2. Count active tasks
                var taskCount = await _context.Tasks.CountAsync(t => t.UserId == request.UserId && t.DeletedAt == null, cancellationToken);
                
                if (taskCount >= 50)
                {
                    throw new ADHDChecklist.API.Entities.Exceptions.PremiumFeatureException(
                        "Bạn đã đạt giới hạn 50 công việc của gói Free. Vui lòng nâng cấp Premium để tạo không giới hạn!", 
                        "Unlimited Tasks");
                }
            }

            // 3. Check for Time Overlaps (Family/Assignment Logic)
            if (request.TimeBlockStart.HasValue && request.TimeBlockEnd.HasValue)
            {
                var targetUserId = request.AssignedUserId ?? request.UserId;
                var start = request.TimeBlockStart.Value;
                var end = request.TimeBlockEnd.Value;

                var hasConflict = await _context.Tasks.AnyAsync(t =>
                    t.DeletedAt == null &&
                    t.ScheduledDate == request.ScheduledDate &&
                    (t.UserId == targetUserId || t.AssignedUserId == targetUserId) && // Check tasks where target is Owner OR Assignee
                    t.TimeBlockStart.HasValue && t.TimeBlockEnd.HasValue &&
                    t.TimeBlockStart < end && t.TimeBlockEnd > start, // Overlap formula
                    cancellationToken);

                if (hasConflict)
                {
                    // Fetch details for better error message
                    var conflictingTask = await _context.Tasks
                        .Where(t =>
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

            var familyId = request.FamilyId;
            if (!familyId.HasValue)
            {
                familyId = await _context.FamilyMembers
                    .Where(fm => fm.UserId == request.UserId)
                    .Select(fm => fm.FamilyId)
                    .FirstOrDefaultAsync(cancellationToken);
                
                if (familyId == Guid.Empty) familyId = null;
            }

            var task = new Entities.Task
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Title = request.Title,
                Description = request.Description,
                CategoryId = request.CategoryId,
                ScheduledDate = request.ScheduledDate,
                TimeBlockStart = request.TimeBlockStart,
                TimeBlockEnd = request.TimeBlockEnd,
                Duration = request.Duration,
                Priority = request.Priority,
                IsRecurring = request.IsRecurring,
                RecurrencePattern = request.RecurrencePattern,
                IsCompleted = false,
                OrderIndex = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DopamineType = request.DopamineType ?? "Low",
                FamilyId = familyId,
                AssignedUserId = request.AssignedUserId ?? request.UserId, // Default to self
                IsShared = request.IsShared,
                AssignmentStatus = (request.AssignedUserId.HasValue && request.AssignedUserId != request.UserId) ? "Pending" : "Accepted",
                IsMandatory = request.IsMandatory
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync(cancellationToken);

            // Load category and assigned user for response
            var entry = _context.Entry(task);
            if (task.CategoryId.HasValue)
            {
                await entry.Reference(t => t.Category).LoadAsync(cancellationToken);
            }
            if (task.AssignedUserId.HasValue)
            {
                await entry.Reference(t => t.AssignedUser).LoadAsync(cancellationToken);
            }

            _logger.LogInformation("Task {TaskId} created by user {UserId}", task.Id, request.UserId);

            // Event: Notify Assignee
            if (task.AssignedUserId.HasValue && task.AssignedUserId != task.UserId)
            {
                // We need the Creator's Name (user.FullName). 
                // We fetched 'user' at the top (line 26).
                // Ensure 'user' variable is available. Yes, line 26.
                
                await _mediator.Publish(new Shared.Events.TaskAssignedEvent(
                    task.Id,
                    task.Title,
                    task.AssignedUserId.Value,
                    task.UserId,
                    user.FullName ?? "Một thành viên"
                ), cancellationToken);
            }

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
                null, // AssignedUserColor
                task.IsShared,
                task.AssignmentStatus,
                task.RejectionReason,
                task.CompletionApprovalStatus, // "None"
                task.IsMandatory,
                task.UserId // CreatorId
            );
        }
    }


    // ============================================
    // ENDPOINT
    // ============================================
    public static class CreateTaskEndpoint
    {
        public static void MapCreateTask(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/tasks", async (
                CreateTaskRequest request,
                ClaimsPrincipal user,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var command = new CreateTaskCommand(
                    request.Title,
                    request.Description,
                    request.CategoryId,
                    request.ScheduledDate,
                    request.TimeBlockStart,
                    request.TimeBlockEnd,
                    request.Duration,
                    request.Priority,
                    request.IsRecurring,
                    request.RecurrencePattern,
                    userId,
                    request.DopamineType,
                    request.FamilyId,
                    request.AssignedUserId,
                    request.IsShared, // Pass IsShared
                    request.IsMandatory
                );

                var result = await mediator.Send(command, ct);

                return Results.Created($"/api/tasks/{result.Id}", result);
            })
            .RequireAuthorization()
            .WithTags("Tasks")
            .WithName("CreateTask")
            .Produces<TaskResponse>(201)
            .Produces(400);
        }
    }
}

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

        public CreateTaskCommandHandler(
            AppDbContext context,
            ILogger<CreateTaskCommandHandler> logger)
        {
            _context = context;
            _logger = logger;
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
                DopamineType = request.DopamineType ?? "Low"
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync(cancellationToken);

            // Load category for response
            if (task.CategoryId.HasValue)
            {
                await _context.Entry(task)
                    .Reference(t => t.Category)
                    .LoadAsync(cancellationToken);
            }

            _logger.LogInformation("Task {TaskId} created by user {UserId}", task.Id, request.UserId);

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
                task.DopamineType
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
                    request.DopamineType
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

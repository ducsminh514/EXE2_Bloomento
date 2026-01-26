using System.Security.Claims;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Shared.DTOs;
using MediatR;

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
                //Priority = (Priority)request.Priority,
                IsRecurring = request.IsRecurring,
                RecurrencePattern = request.RecurrencePattern,
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
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
                (int)task.Priority,
                task.IsRecurring,
                task.RecurrencePattern,
                task.ParentTaskId,
                new List<TaskResponse>(),
                task.OrderIndex,
                task.CreatedAt,
                task.UpdatedAt
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
                Console.WriteLine(userId);
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
                    userId
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

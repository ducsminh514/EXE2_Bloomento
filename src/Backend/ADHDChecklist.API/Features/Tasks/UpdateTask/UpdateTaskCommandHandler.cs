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

        public UpdateTaskCommandHandler(
            AppDbContext context,
            ILogger<UpdateTaskCommandHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TaskResponse?> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _context.Tasks
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == request.TaskId && t.UserId == request.UserId, cancellationToken);

            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found or unauthorized for user {UserId}",
                    request.TaskId, request.UserId);
                return null;
            }

            // Update fields
            task.Title = request.Title;
            task.Description = request.Description;
            task.CategoryId = request.CategoryId;
            task.ScheduledDate = request.ScheduledDate;
            task.TimeBlockStart = request.TimeBlockStart;
            task.TimeBlockEnd = request.TimeBlockEnd;
            task.Duration = request.Duration;
            //task.Priority = (Entities.Priority)request.Priority;
            task.Priority = request.Priority;
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
}

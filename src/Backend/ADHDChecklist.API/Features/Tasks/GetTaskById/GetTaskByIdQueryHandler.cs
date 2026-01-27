using ADHDChecklist.API.Data;
using MediatR;
using ADHDChecklist.API.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
namespace ADHDChecklist.API.Features.Tasks.GetTaskById
{
    public class GetTaskByIdQueryHandler : IRequestHandler<GetTaskByIdQuery, TaskResponse?>
    {
        private readonly AppDbContext _context;
        private readonly ILogger<GetTaskByIdQueryHandler> _logger;

        public GetTaskByIdQueryHandler(
            AppDbContext context,
            ILogger<GetTaskByIdQueryHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TaskResponse?> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
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
                new List<TaskResponse>(), // ✅ Empty list
                task.OrderIndex ?? 0,
                task.CreatedAt,
                task.UpdatedAt
            );
        }
    }

}

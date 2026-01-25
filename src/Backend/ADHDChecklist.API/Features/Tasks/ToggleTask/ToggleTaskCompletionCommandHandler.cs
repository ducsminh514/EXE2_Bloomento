using ADHDChecklist.API.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace ADHDChecklist.API.Features.Tasks.ToggleTask
{
    public class ToggleTaskCompletionCommandHandler : IRequestHandler<ToggleTaskCompletionCommand, bool>
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ToggleTaskCompletionCommandHandler> _logger;

        public ToggleTaskCompletionCommandHandler(AppDbContext context, ILogger<ToggleTaskCompletionCommandHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> Handle(ToggleTaskCompletionCommand request, CancellationToken cancellationToken)
        {
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == request.TaskId && t.UserId == request.UserId, cancellationToken);

            if (task == null)
            {
                return false;
            }

            // Toggle completion
            task.IsCompleted = !task.IsCompleted;
            task.CompletedAt = task.IsCompleted ? DateTime.UtcNow : null;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Task {TaskId} marked as {Status}",
                task.Id, task.IsCompleted ? "completed" : "incomplete");

            return true;
        }
    }
}

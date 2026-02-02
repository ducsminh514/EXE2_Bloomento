using ADHDChecklist.API.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace ADHDChecklist.API.Features.Tasks.ToggleTask
{
    public class ToggleTaskCompletionCommandHandler : IRequestHandler<ToggleTaskCompletionCommand, bool>
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ToggleTaskCompletionCommandHandler> _logger;
        private readonly IMediator _mediator;

        public ToggleTaskCompletionCommandHandler(
            AppDbContext context, 
            ILogger<ToggleTaskCompletionCommandHandler> logger,
            IMediator mediator)
        {
            _context = context;
            _logger = logger;
            _mediator = mediator;
        }

        public async Task<bool> Handle(ToggleTaskCompletionCommand request, CancellationToken cancellationToken)
        {
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == request.TaskId && t.UserId == request.UserId, cancellationToken);

            if (task == null)
            {
                return false;
            }

            bool wasCompleted = task.IsCompleted;
            
            // Toggle completion
            task.IsCompleted = !task.IsCompleted;
            task.CompletedAt = task.IsCompleted ? DateTime.UtcNow : null;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // PUBLISH EVENT IF NEWLY COMPLETED
            if (!wasCompleted && task.IsCompleted)
            {
                // Calculate Points
                int basePoints = 10;
                int priorityBonus = ((task.Priority ?? 1) - 1) * 5;
                int dopamineBonus = task.DopamineType == "Low" ? 10 : 0;
                int totalPoints = basePoints + priorityBonus + dopamineBonus;

                await _mediator.Publish(new Shared.Events.TaskCompletedEvent(
                    task.Id,
                    task.UserId,
                    task.FamilyId,
                    totalPoints
                ), cancellationToken);
            }

            _logger.LogInformation("Task {TaskId} marked as {Status}",
                task.Id, task.IsCompleted ? "completed" : "incomplete");

            return true;
        }
    }
}

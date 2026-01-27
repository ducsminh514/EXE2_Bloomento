using ADHDChecklist.API.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace ADHDChecklist.API.Features.Tasks.MoveTask
{
    public class MoveTaskToInboxHandler : IRequestHandler<MoveTaskToInboxCommand, bool>
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MoveTaskToInboxHandler> _logger;

        public MoveTaskToInboxHandler(AppDbContext context, ILogger<MoveTaskToInboxHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> Handle(MoveTaskToInboxCommand request, CancellationToken cancellationToken)
        {
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == request.TaskId && t.UserId == request.UserId, cancellationToken);

            if (task == null) return false;

            task.TimeBlockStart = null;
            task.TimeBlockEnd = null;
            task.Duration = null;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Task {TaskId} moved to inbox", request.TaskId);
            return true;
        }
    }
}

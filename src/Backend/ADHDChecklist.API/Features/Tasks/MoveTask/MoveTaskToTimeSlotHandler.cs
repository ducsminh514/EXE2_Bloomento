using ADHDChecklist.API.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace ADHDChecklist.API.Features.Tasks.MoveTask
{
    public class MoveTaskToTimeSlotHandler : IRequestHandler<MoveTaskToTimeSlotCommand, bool>
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MoveTaskToTimeSlotHandler> _logger;

        public MoveTaskToTimeSlotHandler(AppDbContext context, ILogger<MoveTaskToTimeSlotHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> Handle(MoveTaskToTimeSlotCommand request, CancellationToken cancellationToken)
        {
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == request.TaskId && t.UserId == request.UserId, cancellationToken);

            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found", request.TaskId);
                return false;
            }

            task.TimeBlockStart = request.TimeBlockStart;
            task.TimeBlockEnd = request.TimeBlockEnd;
            task.Duration = (int)(request.TimeBlockEnd.ToTimeSpan() - request.TimeBlockStart.ToTimeSpan()).TotalMinutes;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Task {TaskId} moved to {Start}-{End}",
                request.TaskId, request.TimeBlockStart, request.TimeBlockEnd);

            return true;
        }
    }
}

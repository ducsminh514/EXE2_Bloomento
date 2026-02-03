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
                .FirstOrDefaultAsync(t => t.Id == request.TaskId && (t.UserId == request.UserId || t.AssignedUserId == request.UserId), cancellationToken);

            if (task == null)
            {
                return false;
            }

            bool wasCompleted = task.IsCompleted;
            
            // LOGIC: Parent Approval
            // Check if Performer is the Assignee BUT NOT the Creator (Owner)
            bool isAssignee = task.AssignedUserId.HasValue && task.AssignedUserId == request.UserId;
            bool isCreator = task.UserId == request.UserId;
            
            // If I am un-completing a task (True -> False)
            if (wasCompleted)
            {
                task.IsCompleted = false;
                task.CompletedAt = null;
                task.CompletionApprovalStatus = "None"; // Reset status
                task.ApproverUserId = null;
            }
            else // Completing (False -> True)
            {
                // If I am the Creator (Parent) completing it -> Done immediately
                // Or if I am assigning to myself -> Done immediately
                if (isCreator) 
                {
                    task.IsCompleted = true;
                    task.CompletedAt = DateTime.UtcNow;
                    task.CompletionApprovalStatus = "Approved";
                    task.ApproverUserId = request.UserId;
                }
                else 
                {
                    // I am the Assignee (Child) -> Set to Pending Approval
                    // DO NOT set IsCompleted = true yet.
                    task.CompletionApprovalStatus = "Pending";
                    
                    // Optional: You might want to track "SubmittedAt"? 
                    // For now, allow checking Pending status.
                    _logger.LogInformation("Task {TaskId} submitted for approval by {UserId}", task.Id, request.UserId);
                    
                    // We return true (success) but task is technically not "IsCompleted" in DB.
                    // Frontend must handle this visual state.
                }
            }

            task.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            // NOTE: We do NOT publish TaskCompletedEvent here anymore.
            // Points are only awarded in ApproveTaskCompletionHandler for parent approval scenarios
            // OR we need to handle "Direct Completion" for parents/self assignments.
            
            // Logic: If task is "Approved" (either automatically by parent/creator or manually later)
            // THEN we should probably award points?
            
            // Wait, if I am the Creator and I complete the task, it gets status "Approved".
            // So we SHOULD award points here IF status is Approved.
            // BUT if status is "Pending", checking "!wasCompleted && task.IsCompleted" is FALSE because we didn't set IsCompleted=true for Pending.
            
            // Re-read logic above:
            // If child: IsCompleted = false, Status = Pending. -> Block below is skipped. Correct.
            // If parent: IsCompleted = true, Status = Approved. -> Block below is entered. Correct.
            
            // So logic is actually fine for Parent case?
            // YES.
            // But user pointed out error: "lỗi chỗ này"
            // Let's re-read the code snippet user sent.
            // It seems the user means I might be using `task.UserId` instead of `task.AssignedUserId`?
            // "task.AssignedUserId ?? task.UserId" looks correct for target.
            
            // However, to be cleaner and avoiding duplicate logic with ApproveHandler, 
            // maybe we can extract point calculation?
            
            // Actually, if I am a Premium user (no points) or just simplified logic...
            // Let's keep it here for "Instant Completion" cases.
            
            // But wait, the previous code block only does `await _context.SaveChangesAsync(cancellationToken);`
            // The `task.IsCompleted` might be false if it is pending!
            
            if (!wasCompleted && task.IsCompleted)
            {
                 // Only run if task is REALLY completed (meaning Parent completed it or Self-completed)
                 // If it is Pending, IsCompleted is false, so this block is skipped.
                 
                 // Calculate Points
                int basePoints = 10;
                int priorityBonus = ((task.Priority ?? 1) - 1) * 5;
                int dopamineBonus = task.DopamineType == "Low" ? 10 : 0;
                int totalPoints = basePoints + priorityBonus + dopamineBonus;

                await _mediator.Publish(new Shared.Events.TaskCompletedEvent(
                    task.Id,
                    task.AssignedUserId ?? task.UserId, // Fix: Ensure we use the Assignee ID if exists
                    task.FamilyId,
                    totalPoints
                ), cancellationToken);
            }

            _logger.LogInformation("Task {TaskId} toggle processed. Status: {Status}, Approval: {Approval}",
                task.Id, task.IsCompleted ? "Completed" : "Incomplete", task.CompletionApprovalStatus);

            return true;
        }
    }
}

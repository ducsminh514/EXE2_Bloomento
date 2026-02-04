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
                bool wasApproved = task.CompletionApprovalStatus == "Approved";

                task.IsCompleted = false;
                task.CompletedAt = null;
                task.CompletionApprovalStatus = "None"; // Reset status
                task.ApproverUserId = null;

                if (wasApproved)
                {
                    // Deduct points if it was already approved
                    await _mediator.Publish(new Shared.Events.TaskCompletedEvent(
                        task.Id,
                        task.AssignedUserId ?? task.UserId,
                        task.FamilyId,
                        -task.CalculateGamificationPoints()
                    ), cancellationToken);
                }
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
                    
                    // Award points immediately
                    await _mediator.Publish(new Shared.Events.TaskCompletedEvent(
                        task.Id,
                        task.AssignedUserId ?? task.UserId,
                        task.FamilyId,
                        task.CalculateGamificationPoints()
                    ), cancellationToken);
                }
                else 
                {
                    // I am the Assignee (Child) -> Set to Pending Approval
                    task.CompletionApprovalStatus = "Pending";
                    _logger.LogInformation("Task {TaskId} submitted for approval by {UserId}", task.Id, request.UserId);
                }
            }

            task.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Task {TaskId} toggle processed. Status: {Status}, Approval: {Approval}",
                task.Id, task.IsCompleted ? "Completed" : "Incomplete", task.CompletionApprovalStatus);

            return true;
        }
    }
}

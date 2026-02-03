using System.Security.Claims;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Shared.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Features.Tasks.ApproveCompletion;

public record ApproveTaskCompletionCommand(Guid TaskId, Guid UserId) : IRequest<bool>;

public class ApproveTaskCompletionCommandHandler : IRequestHandler<ApproveTaskCompletionCommand, bool>
{
    private readonly AppDbContext _context;
    private readonly ILogger<ApproveTaskCompletionCommandHandler> _logger;
    private readonly IMediator _mediator;

    public ApproveTaskCompletionCommandHandler(
        AppDbContext context,
        ILogger<ApproveTaskCompletionCommandHandler> logger,
        IMediator mediator)
    {
        _context = context;
        _logger = logger;
        _mediator = mediator;
    }

    public async Task<bool> Handle(ApproveTaskCompletionCommand request, CancellationToken cancellationToken)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

        if (task == null) return false;

        // AUTH CHECK: 
        // 1. Task Owner (Creator) can always approve.
        // 2. Any Family "Admin" can approve tasks within their family.
        bool isAuthorized = task.UserId == request.UserId;
        
        if (!isAuthorized && task.FamilyId.HasValue)
        {
            var currentUserMember = await _context.FamilyMembers
                .FirstOrDefaultAsync(m => m.FamilyId == task.FamilyId.Value && m.UserId == request.UserId, cancellationToken);
            
            if (currentUserMember != null && (currentUserMember.Role == "Admin" || currentUserMember.Role == "Parent"))
            {
                isAuthorized = true;
            }
        }

        if (!isAuthorized)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền duyệt hoàn thành cho công việc này. Chỉ người tạo hoặc Quản trị viên gia đình mới có quyền.");
        }

        if (task.CompletionApprovalStatus == "Approved" && task.IsCompleted)
        {
            return true; // Already approved
        }

        // Approve
        task.IsCompleted = true;
        task.CompletedAt = DateTime.UtcNow;
        task.CompletionApprovalStatus = "Approved";
        task.ApproverUserId = request.UserId;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Calculate Points
        int basePoints = 10;
        int priorityBonus = ((task.Priority ?? 1) - 1) * 5;
        int dopamineBonus = task.DopamineType == "Low" ? 10 : 0;
        int totalPoints = basePoints + priorityBonus + dopamineBonus;
        
        // Award points to the ASSIGNEE (the child)
        var awardToUserId = task.AssignedUserId ?? task.UserId;

        await _mediator.Publish(new Shared.Events.TaskCompletedEvent(
            task.Id,
            awardToUserId, 
            task.FamilyId,
            totalPoints
        ), cancellationToken);

        _logger.LogInformation("Task {TaskId} APPROVED by {ApproverId}. Points awarded to {AssigneeId}",
            task.Id, request.UserId, awardToUserId);

        return true;
    }
}

public static class ApproveTaskCompletionEndpoint
{
    public static void MapApproveTaskCompletion(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tasks/{taskId}/approve", async (
            Guid taskId,
            ClaimsPrincipal user,
            IMediator mediator) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await mediator.Send(new ApproveTaskCompletionCommand(taskId, userId));
            return result ? Results.Ok() : Results.NotFound();
        })
        .RequireAuthorization()
        .WithTags("Tasks")
        .WithName("ApproveTaskCompletion");
    }
}

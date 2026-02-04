using System.Security.Claims;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Shared.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Features.Tasks.RequestRework;

public record RequestTaskReworkCommand(Guid TaskId, Guid UserId) : IRequest<bool>;

public class RequestTaskReworkCommandHandler : IRequestHandler<RequestTaskReworkCommand, bool>
{
    private readonly AppDbContext _context;
    private readonly ILogger<RequestTaskReworkCommandHandler> _logger;
    private readonly IMediator _mediator;

    public RequestTaskReworkCommandHandler(
        AppDbContext context,
        ILogger<RequestTaskReworkCommandHandler> logger,
        IMediator mediator)
    {
        _context = context;
        _logger = logger;
        _mediator = mediator;
    }

    public async Task<bool> Handle(RequestTaskReworkCommand request, CancellationToken cancellationToken)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

        if (task == null) return false;

        // AUTH CHECK: 
        // 1. Task Owner (Creator) can always request rework.
        // 2. Any Family "Admin" can request rework for tasks within their family.
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
            throw new UnauthorizedAccessException("Bạn không có quyền yêu cầu làm lại cho công việc này. Chỉ người tạo hoặc Quản trị viên gia đình mới có quyền.");
        }

        // Rework Logic
        bool wasApproved = task.CompletionApprovalStatus == "Approved";

        task.IsCompleted = false;
        task.CompletedAt = null;
        task.CompletionApprovalStatus = "ReworkRequested";
        task.ApproverUserId = request.UserId;
        task.UpdatedAt = DateTime.UtcNow;

        if (wasApproved)
        {
            // Deduct points
            await _mediator.Publish(new Shared.Events.TaskCompletedEvent(
                task.Id,
                task.AssignedUserId ?? task.UserId,
                task.FamilyId,
                -task.CalculateGamificationPoints()
            ), cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Task {TaskId} returned for REWORK by {ApproverId}", task.Id, request.UserId);

        return true;
    }
}

public static class RequestTaskReworkEndpoint
{
    public static void MapRequestTaskRework(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tasks/{taskId}/rework", async (
            Guid taskId,
            ClaimsPrincipal user,
            IMediator mediator) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await mediator.Send(new RequestTaskReworkCommand(taskId, userId));
            return result ? Results.Ok() : Results.NotFound();
        })
        .RequireAuthorization()
        .WithTags("Tasks")
        .WithName("RequestTaskRework");
    }
}

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

    public RequestTaskReworkCommandHandler(
        AppDbContext context,
        ILogger<RequestTaskReworkCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(RequestTaskReworkCommand request, CancellationToken cancellationToken)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

        if (task == null) return false;

        // AUTH CHECK
        if (task.UserId != request.UserId)
        {
            throw new UnauthorizedAccessException("Only the task owner can request rework.");
        }

        // Rework Logic
        task.IsCompleted = false;
        task.CompletedAt = null;
        task.CompletionApprovalStatus = "ReworkRequested";
        task.ApproverUserId = request.UserId;
        task.UpdatedAt = DateTime.UtcNow;

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

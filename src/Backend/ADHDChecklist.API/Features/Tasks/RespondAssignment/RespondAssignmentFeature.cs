using MediatR;
using Microsoft.EntityFrameworkCore;
using ADHDChecklist.API.Data;
using System.Security.Claims;
using FluentValidation;
using ADHDChecklist.API.Services;

namespace ADHDChecklist.API.Features.Tasks.RespondAssignment;

public record RespondAssignmentRequest(string Status, string? Reason = null); // "Accepted", "Rejected"

public record Command(Guid TaskId, string Status, string? Reason = null) : IRequest<bool>;

public class RespondAssignmentValidator : AbstractValidator<Command>
{
    public RespondAssignmentValidator()
    {
        RuleFor(x => x.Status).Must(s => s == "Accepted" || s == "Rejected")
            .WithMessage("Status must be 'Accepted' or 'Rejected'");
        
        RuleFor(x => x.Reason).NotEmpty().When(x => x.Status == "Rejected")
            .WithMessage("Lý do từ chối là bắt buộc.");
    }
}

public static class RespondAssignmentEndpoint
{
    public static void MapRespondAssignment(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tasks/{id}/respond", async (
            Guid id,
            RespondAssignmentRequest request,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            try
            {
                var success = await mediator.Send(new Command(id, request.Status, request.Reason), ct);
                return success ? Results.Ok() : Results.NotFound();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .RequireAuthorization()
        .WithTags("Tasks")
        .WithName("RespondAssignment");
    }
}

public class Handler : IRequestHandler<Command, bool>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService; // Assuming ICurrentUserService exists and provides UserId

    public Handler(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId)) return false;

        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

        if (task == null) return false;

        // Only assigned user can respond
        if (task.AssignedUserId.ToString() != userId) return false;

        if (request.Status == "Rejected" && task.IsMandatory)
        {
            throw new InvalidOperationException("Công việc này là BẮT BUỘC (Mandatory), bạn không thể từ chối!");
        }

        if (request.Status == "Accepted")
        {
            task.AssignmentStatus = "Accepted";
            task.RejectionReason = null; // Clear rejection reason if accepted
        }
        else if (request.Status == "Rejected")
        {
            task.AssignmentStatus = "Rejected";
            task.RejectionReason = request.Reason; // Save reason
            // Optional: Unassign user? Or keep it for history? User wants it to "disappear" from their list.
            // We keep assignment so Rejection shows up for Assigner. Filtering will be done on Query side.
        }

        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}







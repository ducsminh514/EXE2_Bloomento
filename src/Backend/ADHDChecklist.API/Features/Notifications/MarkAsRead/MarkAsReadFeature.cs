using System.Security.Claims;
using ADHDChecklist.API.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Features.Notifications.MarkAsRead;

public record MarkAsReadCommand(Guid NotificationId, Guid UserId) : IRequest<bool>;

public class MarkAsReadHandler : IRequestHandler<MarkAsReadCommand, bool>
{
    private readonly AppDbContext _context;

    public MarkAsReadHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.UserId == request.UserId, cancellationToken);

        if (notification == null) return false;

        notification.IsRead = true;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public static class MarkAsReadEndpoint
{
    public static void MapMarkAsRead(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/notifications/{id:guid}/read", async (
            Guid id,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId)) return Results.Unauthorized();

            var success = await mediator.Send(new MarkAsReadCommand(id, userId), ct);
            
            return success ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization()
        .WithTags("Notifications")
        .WithName("MarkNotificationRead");
    }
}

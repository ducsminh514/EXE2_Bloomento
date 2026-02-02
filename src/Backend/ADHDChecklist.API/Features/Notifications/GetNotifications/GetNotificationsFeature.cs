using System.Security.Claims;
using ADHDChecklist.API.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Features.Notifications.GetNotifications;

public record NotificationResponse(
    Guid Id,
    string Title,
    string Message,
    string Type,
    Guid? ReferenceId,
    bool IsRead,
    DateTime CreatedAt
);

public record GetNotificationsQuery(Guid UserId) : IRequest<List<NotificationResponse>>;

public class GetNotificationsHandler : IRequestHandler<GetNotificationsQuery, List<NotificationResponse>>
{
    private readonly AppDbContext _context;

    public GetNotificationsHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<NotificationResponse>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .Where(n => n.UserId == request.UserId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50) // Limit to last 50
            .Select(n => new NotificationResponse(
                n.Id,
                n.Title,
                n.Message,
                n.Type,
                n.ReferenceId,
                n.IsRead,
                n.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }
}

public static class GetNotificationsEndpoint
{
    public static void MapGetNotifications(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/notifications", async (
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new GetNotificationsQuery(userId), ct);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithTags("Notifications")
        .WithName("GetNotifications");
    }
}

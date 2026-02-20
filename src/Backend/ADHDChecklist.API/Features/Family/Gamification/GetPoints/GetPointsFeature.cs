using System.Security.Claims;
using ADHDChecklist.API.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Features.Family.Gamification.GetPoints;

public record GetPointsQuery(Guid UserId) : IRequest<int>;

public class GetPointsHandler : IRequestHandler<GetPointsQuery, int>
{
    private readonly AppDbContext _context;

    public GetPointsHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(GetPointsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Users
            .Where(u => u.Id == request.UserId)
            .Select(u => u.TotalXp)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

public static class GetPointsEndpoint
{
    public static void MapGetPoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/family/points", async (
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId)) return Results.Unauthorized();

            // Total points across all history (earnings and redemptions)
            var points = await mediator.Send(new GetPointsQuery(userId), ct);
            return Results.Ok(new { points });
        })
        .RequireAuthorization()
        .WithTags("Family Gamification")
        .WithName("GetFamilyPoints");
    }
}

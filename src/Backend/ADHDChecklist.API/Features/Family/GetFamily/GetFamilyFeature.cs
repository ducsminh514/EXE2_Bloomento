using MediatR;
using Microsoft.EntityFrameworkCore;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using System.Security.Claims;

namespace ADHDChecklist.API.Features.Family.GetFamily;

public record GetFamilyQuery(Guid UserId) : IRequest<FamilyResponse?>;

public record FamilyResponse(
    Guid Id, 
    string Name, 
    Guid OwnerId, 
    bool IsOwner,
    List<FamilyMemberResponse> Members
);

public record FamilyMemberResponse(
    Guid UserId, 
    string FullName, 
    string Role, 
    string? Nickname,
    string? AvatarUrl
);

public class GetFamilyHandler : IRequestHandler<GetFamilyQuery, FamilyResponse?>
{
    private readonly AppDbContext _context;

    public GetFamilyHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<FamilyResponse?> Handle(GetFamilyQuery request, CancellationToken cancellationToken)
    {
        // Find the family the user belongs to
        var memberRecord = await _context.FamilyMembers
            .Include(m => m.Family)
                .ThenInclude(f => f.Members)
                    .ThenInclude(m => m.User) // Include User to get details
            .FirstOrDefaultAsync(m => m.UserId == request.UserId, cancellationToken);
            
        if (memberRecord == null)
        {
            return null;
        }

        var family = memberRecord.Family;
        var isOwner = family.OwnerId == request.UserId;

        var members = family.Members.Select(m => new FamilyMemberResponse(
            m.UserId,
            m.User.FullName ?? "Unknown",
            m.Role,
            m.Nickname,
            m.User.GoogleProfilePicture // Assuming this exists or falls back
        )).ToList();

        return new FamilyResponse(
            family.Id,
            family.Name,
            family.OwnerId,
            isOwner,
            members
        );
    }
}

public static class GetFamilyEndpoint
{
    public static void MapGetFamily(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/family", async (
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId)) return Results.Unauthorized();

            var result = await mediator.Send(new GetFamilyQuery(userId), ct);
            
            // Return 200 OK with null if no family found, simpler for frontend to handle than 404
            // Return 404 Not Found if no family found, so client can handle it gracefully
            return result != null ? Results.Ok(result) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithTags("Family")
        .WithName("GetFamily");
    }
}

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using ADHDChecklist.API.Entities.Common;
using System.Security.Claims;

namespace ADHDChecklist.API.Features.Family.JoinFamily;

public record JoinFamilyRequest(string InviteCode);

public record JoinFamilyCommand(string InviteCode, Guid UserId) : IRequest<Guid>;

public class JoinFamilyHandler : IRequestHandler<JoinFamilyCommand, Guid>
{
    private readonly AppDbContext _context;

    public JoinFamilyHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(JoinFamilyCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.FamilyMembers)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
            
        if (user == null) throw new UnauthorizedAccessException("User not found");

        if (user.FamilyMembers.Any())
        {
            throw new InvalidOperationException("You are already a member of a family.");
        }

        // Validate Code
        var invitation = await _context.FamilyInvitations
            .Include(i => i.Family)
            .FirstOrDefaultAsync(i => i.Code == request.InviteCode 
                && i.Status == "Pending" 
                && i.ExpiresAt > DateTime.UtcNow, cancellationToken);

        if (invitation == null)
        {
            throw new InvalidOperationException("Invitation code is invalid or expired.");
        }

        // Add Member
        var member = new FamilyMember
        {
            Id = Guid.NewGuid(),
            FamilyId = invitation.FamilyId,
            UserId = request.UserId,
            Role = "Member",
            JoinedAt = DateTime.UtcNow,
            Nickname = user.FullName
        };

        invitation.Status = "Accepted";
        
        // Upgrade User
        user.PreviousSubscriptionTier = (SubscriptionTier)user.SubscriptionTier;
        user.SubscriptionTier = SubscriptionTier.Family;

        _context.FamilyMembers.Add(member);
        
        await _context.SaveChangesAsync(cancellationToken);

        return invitation.FamilyId;
    }
}

public static class JoinFamilyEndpoint
{
    public static void MapJoinFamily(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/family/join", async (
            JoinFamilyRequest request,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId)) return Results.Unauthorized();

            try
            {
                var familyId = await mediator.Send(new JoinFamilyCommand(request.InviteCode, userId), ct);
                return Results.Ok(familyId);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .RequireAuthorization()
        .WithTags("Family")
        .WithName("JoinFamily");
    }
}

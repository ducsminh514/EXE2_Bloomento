using MediatR;
using Microsoft.EntityFrameworkCore;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using ADHDChecklist.API.Entities.Common;
using System.Security.Claims;

namespace ADHDChecklist.API.Features.Family.RemoveMember;

public record RemoveMemberCommand(Guid FamilyId, Guid MemberId, Guid CurrentUserId) : IRequest<bool>;

public class RemoveMemberHandler : IRequestHandler<RemoveMemberCommand, bool>
{
    private readonly AppDbContext _context;

    public RemoveMemberHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate Admin (Current User)
        var currentUserMember = await _context.FamilyMembers
            .FirstOrDefaultAsync(m => m.FamilyId == request.FamilyId && m.UserId == request.CurrentUserId, cancellationToken);

        if (currentUserMember == null || currentUserMember.Role != "Admin")
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xóa thành viên.");
        }

        // 2. Prevent removing self (Admin)
        if (request.MemberId == request.CurrentUserId)
        {
             throw new InvalidOperationException("Bạn không thể tự xóa chính mình. Hãy dùng chức năng Rời gia đình hoặc Xóa gia đình.");
        }

        // 3. Find Target Member
        var targetMember = await _context.FamilyMembers
            .FirstOrDefaultAsync(m => m.FamilyId == request.FamilyId && m.UserId == request.MemberId, cancellationToken);

        if (targetMember == null)
        {
            throw new InvalidOperationException("Thành viên không tồn tại trong gia đình này.");
        }

        // 4. Find Target User to downgrade
        var targetUser = await _context.Users.FindAsync(new object[] { request.MemberId }, cancellationToken);
        
        if (targetUser != null)
        {
            // Downgrade Logic
            if (targetUser.PreviousSubscriptionTier.HasValue)
            {
                targetUser.SubscriptionTier = targetUser.PreviousSubscriptionTier.Value;
                // Optional: Clear previous tier after restoring? 
                // targetUser.PreviousSubscriptionTier = null; // Keep it or clear logic? Clearing is safer to avoid loops.
                targetUser.PreviousSubscriptionTier = null; 
            }
            else
            {
                targetUser.SubscriptionTier = SubscriptionTier.Free;
            }
        }

        // 5. Remove Member
        _context.FamilyMembers.Remove(targetMember);
        
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}

public static class RemoveMemberEndpoint
{
    public static void MapRemoveMember(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/family/{familyId}/members/{memberId}", async (
            Guid familyId,
            Guid memberId,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId)) return Results.Unauthorized();

            try
            {
                await mediator.Send(new RemoveMemberCommand(familyId, memberId, userId), ct);
                return Results.Ok();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Problem(ex.Message, statusCode: 403);
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
        .WithName("RemoveMember");
    }
}

using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ADHDChecklist.API.Features.Family.UpdateMemberRole;

public record UpdateMemberRoleRequest(string Role);

public record UpdateMemberRoleCommand(Guid FamilyId, Guid MemberId, string Role, Guid CurrentUserId) : IRequest<bool>;

public class UpdateMemberRoleHandler : IRequestHandler<UpdateMemberRoleCommand, bool>
{
    private readonly AppDbContext _context;

    public UpdateMemberRoleHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateMemberRoleCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate permissions (Only Family Owner can change roles)
        var family = await _context.Families
            .FirstOrDefaultAsync(f => f.Id == request.FamilyId, cancellationToken);

        if (family == null)
        {
            throw new InvalidOperationException("Gia đình không tồn tại.");
        }

        if (family.OwnerId != request.CurrentUserId)
        {
            throw new UnauthorizedAccessException("Chỉ Chủ sở hữu gia đình mới có quyền thay đổi vai trò thành viên.");
        }

        // 2. Find Target Member
        var targetMember = await _context.FamilyMembers
            .FirstOrDefaultAsync(m => m.FamilyId == request.FamilyId && m.UserId == request.MemberId, cancellationToken);

        if (targetMember == null)
        {
            throw new InvalidOperationException("Thành viên không tồn tại trong gia đình này.");
        }

        // 3. Prevent changing owner's role
        if (targetMember.UserId == family.OwnerId)
        {
            throw new InvalidOperationException("Không thể thay đổi vai trò của Chủ sở hữu gia đình.");
        }

        // 4. Update Role
        // Valid roles: "Admin", "Member", "Child" (based on FamilyMember.cs comments)
        var validRoles = new[] { "Admin", "Member", "Child" };
        if (!validRoles.Contains(request.Role))
        {
            throw new InvalidOperationException($"Vai trò '{request.Role}' không hợp lệ. Các vai trò cho phép: Admin, Member, Child.");
        }

        targetMember.Role = request.Role;
        
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}

public static class UpdateMemberRoleEndpoint
{
    public static void MapUpdateMemberRole(this IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/family/{familyId}/members/{memberId}/role", async (
            Guid familyId,
            Guid memberId,
            UpdateMemberRoleRequest request,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId)) return Results.Unauthorized();

            try
            {
                await mediator.Send(new UpdateMemberRoleCommand(familyId, memberId, request.Role, userId), ct);
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
        .WithName("UpdateMemberRole");
    }
}

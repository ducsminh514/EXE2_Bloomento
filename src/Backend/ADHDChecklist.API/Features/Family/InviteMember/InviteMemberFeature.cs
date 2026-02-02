using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using System.Security.Claims;
using System.Security.Cryptography;

namespace ADHDChecklist.API.Features.Family.InviteMember;

public record InviteMemberRequest(string Email);

public record InviteMemberCommand(string Email, Guid UserId) : IRequest<string>;

public class InviteMemberValidator : AbstractValidator<InviteMemberCommand>
{
    public InviteMemberValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public class InviteMemberHandler : IRequestHandler<InviteMemberCommand, string>
{
    private readonly AppDbContext _context;

    public InviteMemberHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string> Handle(InviteMemberCommand request, CancellationToken cancellationToken)
    {
        // Check if inviter is Admin
        var memberRecord = await _context.FamilyMembers
            .Include(m => m.Family)
            .FirstOrDefaultAsync(m => m.UserId == request.UserId, cancellationToken);
            
        if (memberRecord == null || memberRecord.Role != "Admin")
        {
            throw new UnauthorizedAccessException("Only family admins can invite members.");
        }

        // Generate Code
        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        
        var invitation = new FamilyInvitation
        {
            Id = Guid.NewGuid(),
            FamilyId = memberRecord.FamilyId,
            Email = request.Email,
            Code = code,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(2)
        };

        _context.FamilyInvitations.Add(invitation);
        
        await _context.SaveChangesAsync(cancellationToken);

        return code;
    }
}

public static class InviteMemberEndpoint
{
    public static void MapInviteMember(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/family/invite", async (
            InviteMemberRequest request,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId)) return Results.Unauthorized();

            try
            {
                var code = await mediator.Send(new InviteMemberCommand(request.Email, userId), ct);
                return Results.Ok(new { Code = code });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Problem(ex.Message, statusCode: 403);
            }
            catch (Exception ex)
            {
                 return Results.BadRequest(new { message = ex.Message });
            }
        })
        .RequireAuthorization()
        .WithTags("Family")
        .WithName("InviteMember");
    }
}

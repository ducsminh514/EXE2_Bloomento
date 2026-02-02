using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using System.Security.Claims;
using System.Security.Cryptography;
using ADHDChecklist.API.Services;
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
    private readonly IEmailService _emailService;

    public InviteMemberHandler(AppDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<string> Handle(InviteMemberCommand request, CancellationToken cancellationToken)
    {
        // Check if inviter is Admin
        var memberRecord = await _context.FamilyMembers
            .Include(m => m.Family)
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.UserId == request.UserId, cancellationToken);
            
        if (memberRecord == null || memberRecord.Role != "Admin")
        {
            throw new UnauthorizedAccessException("Chỉ có Quản trị viên gia đình mới có thể mời thành viên.");
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
            ExpiresAt = DateTime.UtcNow.AddDays(7) // Increased to 7 days
        };

        _context.FamilyInvitations.Add(invitation);
        await _context.SaveChangesAsync(cancellationToken);

        // Send Email
        try
        {
            await _emailService.SendFamilyInvitationAsync(
                request.Email, 
                memberRecord.Family.Name, 
                memberRecord.User.FullName ?? "Thành viên gia đình", 
                code);
        }
        catch (Exception ex)
        {
            // Log error but don't fail the invitation creation
            Console.WriteLine($"Lỗi gửi email mời: {ex.Message}");
        }

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

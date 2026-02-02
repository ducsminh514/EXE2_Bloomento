using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using ADHDChecklist.API.Entities.Common;
using System.Security.Claims;

namespace ADHDChecklist.API.Features.Family.CreateFamily;

public record CreateFamilyRequest(string Name);

public record CreateFamilyCommand(string Name, Guid UserId) : IRequest<Guid>;

public class CreateFamilyValidator : AbstractValidator<CreateFamilyCommand>
{
    public CreateFamilyValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class CreateFamilyHandler : IRequestHandler<CreateFamilyCommand, Guid>
{
    private readonly AppDbContext _context;

    public CreateFamilyHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateFamilyCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.FamilyMembers)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
            
        if (user == null)
        {
             throw new UnauthorizedAccessException("User not found");
        }

        // Check if user is already in a family
        if (user.FamilyMembers.Any())
        {
            throw new InvalidOperationException("You are already a member of a family. Leave your current family to create a new one.");
        }

        // Create Family
        var family = new Entities.Family
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            OwnerId = request.UserId,
            SubscriptionPlan = 2, // Family Tier
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Create Member Record (Admin)
        var member = new FamilyMember
        {
            Id = Guid.NewGuid(),
            FamilyId = family.Id,
            UserId = request.UserId,
            Role = "Admin",
            JoinedAt = DateTime.UtcNow,
            Nickname = user.FullName // Default nickname
        };
        
        family.Members.Add(member);
        
        // Upgrade User Subscription
        user.SubscriptionTier = SubscriptionTier.Family; 
        
        _context.Families.Add(family);
        
        await _context.SaveChangesAsync(cancellationToken);

        return family.Id;
    }
}

public static class CreateFamilyEndpoint
{
    public static void MapCreateFamily(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/family", async (
            CreateFamilyRequest request,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId)) return Results.Unauthorized();

            try 
            {
                var command = new CreateFamilyCommand(request.Name, userId);
                var familyId = await mediator.Send(command, ct);
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
        .WithName("CreateFamily");
    }
}

using System.Security.Claims;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Features.Family.Gamification.ManageRewards;

// DTOs
public record RewardResponse(Guid Id, string Title, string? Description, int CostPoints, bool IsAvailable);
public record CreateRewardRequest(string Title, string? Description, int CostPoints);
public record RedeemRewardRequest(Guid RewardId);

// --- 1. GET REWARDS ---
public record GetRewardsQuery(Guid UserId) : IRequest<List<RewardResponse>>;

public class GetRewardsHandler : IRequestHandler<GetRewardsQuery, List<RewardResponse>>
{
    private readonly AppDbContext _context;

    public GetRewardsHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<RewardResponse>> Handle(GetRewardsQuery request, CancellationToken cancellationToken)
    {
        // Find user's family
        var familyMember = await _context.FamilyMembers
            .FirstOrDefaultAsync(fm => fm.UserId == request.UserId, cancellationToken);
        
        if (familyMember == null) return new List<RewardResponse>();

        return await _context.FamilyRewards
            .Where(r => r.FamilyId == familyMember.FamilyId)
            .OrderBy(r => r.CostPoints)
            .Select(r => new RewardResponse(r.Id, r.Title, r.Description, r.CostPoints, r.IsAvailable))
            .ToListAsync(cancellationToken);
    }
}

// --- 2. CREATE REWARD ---
public record CreateRewardCommand(Guid UserId, string Title, string? Description, int CostPoints) : IRequest<RewardResponse?>;

public class CreateRewardHandler : IRequestHandler<CreateRewardCommand, RewardResponse?>
{
    private readonly AppDbContext _context;

    public CreateRewardHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<RewardResponse?> Handle(CreateRewardCommand request, CancellationToken cancellationToken)
    {
        var familyMember = await _context.FamilyMembers
            .FirstOrDefaultAsync(fm => fm.UserId == request.UserId, cancellationToken);

        if (familyMember == null || (familyMember.Role != "Admin" && familyMember.Role != "Parent"))
        {
            throw new UnauthorizedAccessException("Chỉ có Quản trị viên/Phụ huynh mới có thể tạo phần thưởng.");
        }

        var reward = new FamilyReward
        {
            Id = Guid.NewGuid(),
            FamilyId = familyMember.FamilyId,
            Title = request.Title,
            Description = request.Description,
            CostPoints = request.CostPoints,
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.FamilyRewards.Add(reward);
        await _context.SaveChangesAsync(cancellationToken);

        return new RewardResponse(reward.Id, reward.Title, reward.Description, reward.CostPoints, reward.IsAvailable);
    }
}

// --- 3. REDEEM REWARD ---
public record RedeemRewardCommand(Guid UserId, Guid RewardId) : IRequest<bool>;

public class RedeemRewardHandler : IRequestHandler<RedeemRewardCommand, bool>
{
    private readonly AppDbContext _context;

    public RedeemRewardHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(RedeemRewardCommand request, CancellationToken cancellationToken)
    {
        var familyMember = await _context.FamilyMembers
            .FirstOrDefaultAsync(fm => fm.UserId == request.UserId, cancellationToken);
        
        if (familyMember == null) return false;

        var reward = await _context.FamilyRewards
            .FirstOrDefaultAsync(r => r.Id == request.RewardId && r.FamilyId == familyMember.FamilyId, cancellationToken);
        
        if (reward == null || !reward.IsAvailable) return false;

        // Check if user has enough points
        var currentPoints = await _context.FamilyPointHistory
            .Where(ph => ph.UserId == request.UserId)
            .SumAsync(ph => ph.Amount, cancellationToken);

        if (currentPoints < reward.CostPoints)
        {
            throw new InvalidOperationException($"Bạn không đủ điểm để đổi phần thưởng này. Cần {reward.CostPoints} điểm, bạn hiện có {currentPoints} điểm.");
        }

        // Deduct points
        var redemption = new FamilyPointHistory
        {
            Id = Guid.NewGuid(),
            FamilyId = familyMember.FamilyId,
            UserId = request.UserId,
            Amount = -reward.CostPoints, // Negative for redemption
            Source = "Redemption",
            ReferenceId = reward.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.FamilyPointHistory.Add(redemption);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}

// --- ENDPOINT ---
public static class ManageRewardsEndpoint
{
    public static void MapManageRewards(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/family/rewards").RequireAuthorization().WithTags("Family Gamification");

        group.MapGet("/", async (ClaimsPrincipal user, IMediator mediator) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await mediator.Send(new GetRewardsQuery(userId));
            return Results.Ok(result);
        }).WithName("GetFamilyRewards");

        group.MapPost("/", async (CreateRewardRequest request, ClaimsPrincipal user, IMediator mediator) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await mediator.Send(new CreateRewardCommand(userId, request.Title, request.Description, request.CostPoints));
            return Results.Created($"/api/family/rewards/{result?.Id}", result);
        }).WithName("CreateFamilyReward");

        group.MapPost("/redeem", async (RedeemRewardRequest request, ClaimsPrincipal user, IMediator mediator) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var success = await mediator.Send(new RedeemRewardCommand(userId, request.RewardId));
            return success ? Results.Ok(new { message = "Đổi phần thưởng thành công!" }) : Results.BadRequest("Không thể đổi phần thưởng.");
        }).WithName("RedeemFamilyReward");
    }
}

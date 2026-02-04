using System.Security.Claims;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Features.Family.Gamification.ManageRewards;

// DTOs
public record RewardResponse(Guid Id, string Title, string? Description, int CostPoints, bool IsAvailable, string Status, Guid? CreatorId, string? CreatorName, string? RejectionReason, DateTime CreatedAt);
public record CreateRewardRequest(string Title, string? Description, int CostPoints);
public record ApproveRewardRequest(int CostPoints, bool Approved, string? RejectionReason);
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
            .Include(r => r.Creator) 
            .Where(r => r.FamilyId == familyMember.FamilyId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RewardResponse(
                r.Id, 
                r.Title, 
                r.Description, 
                r.CostPoints, 
                r.IsAvailable, 
                r.Status, 
                r.CreatorId,
                r.Creator != null ? r.Creator.FullName : null,
                r.RejectionReason,
                r.CreatedAt)) // Include RejectionReason
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

        if (familyMember == null) throw new UnauthorizedAccessException("Bạn không thuộc gia đình nào.");

        bool isAdmin = familyMember.Role == "Admin" || familyMember.Role == "Parent";

        var reward = new FamilyReward
        {
            Id = Guid.NewGuid(),
            FamilyId = familyMember.FamilyId,
            Title = request.Title,
            Description = request.Description,
            CostPoints = request.CostPoints, // Negotiation: Always allow saving proposed points
            IsAvailable = isAdmin, // Active only if admin creates it
            Status = isAdmin ? "Active" : "PendingApproval",
            CreatorId = request.UserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.FamilyRewards.Add(reward);
        await _context.SaveChangesAsync(cancellationToken);
 
        // Get creator name for response
        var creatorName = await _context.Users
            .Where(u => u.Id == request.UserId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken);

        return new RewardResponse(reward.Id, reward.Title, reward.Description, reward.CostPoints, reward.IsAvailable, reward.Status, reward.CreatorId, creatorName, null, reward.CreatedAt);
    }
}

// --- 3. APPROVE REWARD [NEW] ---
public record ApproveRewardCommand(Guid UserId, Guid RewardId, int CostPoints, bool Approved, string? RejectionReason) : IRequest<bool>;

public class ApproveRewardHandler : IRequestHandler<ApproveRewardCommand, bool>
{
    private readonly AppDbContext _context;

    public ApproveRewardHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(ApproveRewardCommand request, CancellationToken cancellationToken)
    {
        var familyMember = await _context.FamilyMembers
            .FirstOrDefaultAsync(fm => fm.UserId == request.UserId, cancellationToken);

        if (familyMember == null || (familyMember.Role != "Admin" && familyMember.Role != "Parent"))
        {
            throw new UnauthorizedAccessException("Chỉ có Quản trị viên/Phụ huynh mới có thể duyệt phần thưởng.");
        }

        var reward = await _context.FamilyRewards
            .FirstOrDefaultAsync(r => r.Id == request.RewardId && r.FamilyId == familyMember.FamilyId, cancellationToken);

        if (reward == null) return false;

        if (request.Approved)
        {
            reward.Status = "Active";
            reward.CostPoints = request.CostPoints;
            reward.IsAvailable = true;
            reward.RejectionReason = null; // Clear rejection reason if approved
        }
        else
        {
            reward.Status = "Rejected";
            reward.IsAvailable = false;
            reward.RejectionReason = request.RejectionReason; // Save rejection reason
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
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

        group.MapPost("/{rewardId}/approve", async (Guid rewardId, ApproveRewardRequest request, ClaimsPrincipal user, IMediator mediator) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var success = await mediator.Send(new ApproveRewardCommand(userId, rewardId, request.CostPoints, request.Approved, request.RejectionReason));
            return success ? Results.Ok() : Results.NotFound();
        }).WithName("ApproveFamilyReward");
    }
}

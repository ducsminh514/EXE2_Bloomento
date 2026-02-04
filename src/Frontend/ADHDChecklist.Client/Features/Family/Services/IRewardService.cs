using ADHDChecklist.Client.Shared.Models;

namespace ADHDChecklist.Client.Features.Family.Services;

public interface IRewardService
{
    Task<int> GetPointsAsync();
    Task<List<RewardResponse>> GetRewardsAsync();
    Task<RewardResponse?> CreateRewardAsync(CreateRewardRequest request);
    Task<bool> RedeemRewardAsync(Guid rewardId);
    Task<bool> ApproveRewardAsync(Guid rewardId, ApproveRewardRequest request);
}

public class RewardResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CostPoints { get; set; }
    public bool IsAvailable { get; set; }
    public string Status { get; set; } = "Active";
    public Guid? CreatorId { get; set; }
    public string? CreatorName { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ApproveRewardRequest
{
    public int CostPoints { get; set; }
    public bool Approved { get; set; }
    public string? RejectionReason { get; set; }
}

public class CreateRewardRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CostPoints { get; set; } = 100;
}

public class RedeemRewardRequest
{
    public Guid RewardId { get; set; }
    public RedeemRewardRequest(Guid rewardId) => RewardId = rewardId;
    public RedeemRewardRequest() { }
}

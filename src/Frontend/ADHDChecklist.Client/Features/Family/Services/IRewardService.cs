using ADHDChecklist.Client.Shared.Models;

namespace ADHDChecklist.Client.Features.Family.Services;

public interface IRewardService
{
    Task<int> GetPointsAsync();
    Task<List<RewardResponse>> GetRewardsAsync();
    Task<RewardResponse?> CreateRewardAsync(CreateRewardRequest request);
    Task<bool> RedeemRewardAsync(Guid rewardId);
}

public class RewardResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CostPoints { get; set; }
    public bool IsAvailable { get; set; }
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

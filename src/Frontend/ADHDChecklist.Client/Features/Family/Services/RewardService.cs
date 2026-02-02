using System.Net.Http.Json;
using ADHDChecklist.Client.Shared.Models;
using ADHDChecklist.Client.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace ADHDChecklist.Client.Features.Family.Services;

public class RewardService : IRewardService
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<RewardService> _logger;

    public RewardService(IApiClient apiClient, ILogger<RewardService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<int> GetPointsAsync()
    {
        try 
        {
            var response = await _apiClient.GetAsync<PointsResponse>("api/family/points");
            return response?.Points ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting points");
            return 0;
        }
    }

    public async Task<List<RewardResponse>> GetRewardsAsync()
    {
        try 
        {
            return await _apiClient.GetAsync<List<RewardResponse>>("api/family/rewards") ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rewards");
            return new();
        }
    }

    public async Task<RewardResponse?> CreateRewardAsync(CreateRewardRequest request)
    {
        return await _apiClient.PostAsync<RewardResponse>("api/family/rewards", request);
    }

    public async Task<bool> RedeemRewardAsync(Guid rewardId)
    {
        try 
        {
            await _apiClient.PostAsync<object>("api/family/rewards/redeem", new RedeemRewardRequest(rewardId));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private record PointsResponse(int Points);
}

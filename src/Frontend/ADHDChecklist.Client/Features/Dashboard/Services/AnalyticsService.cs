using ADHDChecklist.Client.Infrastructure.Services;
using ADHDChecklist.Client.Shared.Models;

namespace ADHDChecklist.Client.Features.Dashboard.Services
{
    public interface IAnalyticsService
    {
        Task<WeeklyAnalyticsResponse?> GetWeeklyAnalyticsAsync();
    }

    public class AnalyticsService : IAnalyticsService
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<AnalyticsService> _logger;

        public AnalyticsService(IApiClient apiClient, ILogger<AnalyticsService> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task<WeeklyAnalyticsResponse?> GetWeeklyAnalyticsAsync()
        {
            try
            {
                return await _apiClient.GetAsync<WeeklyAnalyticsResponse>("/api/analytics/weekly");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting analytics");
                return null;
            }
        }
    }
}

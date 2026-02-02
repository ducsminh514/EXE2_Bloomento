using ADHDChecklist.Client.Shared.Models;
using ADHDChecklist.Client.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace ADHDChecklist.Client.Features.Habits.Services
{
    public class HabitService : IHabitService
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<HabitService> _logger;

        public HabitService(IApiClient apiClient, ILogger<HabitService> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task<List<HabitResponse>> GetHabitsAsync()
        {
            try
            {
                return await _apiClient.GetAsync<List<HabitResponse>>("api/habits") 
                       ?? new List<HabitResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting habits");
                return new List<HabitResponse>();
            }
        }

        public async Task<Guid> CreateHabitAsync(CreateHabitRequest request)
        {
            var response = await _apiClient.PostAsync<Guid>("api/habits", request);
            return response;
        }

        public async Task<bool> ToggleHabitAsync(Guid habitId, DateOnly date)
        {
            try 
            {
                var request = new ToggleHabitRequest(date);
                var result = await _apiClient.PostAsync<bool>($"api/habits/{habitId}/toggle", request);
                return result;
            }
            catch
            {
                return false;
            }
        }
    }
}

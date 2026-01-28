using ADHDChecklist.Client.Shared.Models;
using System.Net.Http.Json;

namespace ADHDChecklist.Client.Features.Habits.Services
{
    public class HabitService : IHabitService
    {
        private readonly HttpClient _httpClient;

        public HabitService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<HabitResponse>> GetHabitsAsync()
        {
            try
            {
                // Returns 200 OK with List<HabitResponse> or empty list
                return await _httpClient.GetFromJsonAsync<List<HabitResponse>>("api/habits") 
                       ?? new List<HabitResponse>();
            }
            catch
            {
                return new List<HabitResponse>();
            }
        }

        public async Task<Guid> CreateHabitAsync(CreateHabitRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/habits", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Guid>();
            }
            throw new Exception("Failed to create habit");
        }

        public async Task<bool> ToggleHabitAsync(Guid habitId, DateOnly date)
        {
            var request = new ToggleHabitRequest(date);
            var response = await _httpClient.PostAsJsonAsync($"api/habits/{habitId}/toggle", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<bool>();
            }
            return false;
        }
    }
}

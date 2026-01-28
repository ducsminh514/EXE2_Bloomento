using ADHDChecklist.Client.Shared.Models;

namespace ADHDChecklist.Client.Features.Habits.Services
{
    public interface IHabitService
    {
        Task<List<HabitResponse>> GetHabitsAsync();
        Task<Guid> CreateHabitAsync(CreateHabitRequest request);
        Task<bool> ToggleHabitAsync(Guid habitId, DateOnly date);
        // Task DeleteHabitAsync(Guid habitId);
    }
}

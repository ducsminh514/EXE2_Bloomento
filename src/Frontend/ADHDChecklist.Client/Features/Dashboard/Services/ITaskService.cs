using ADHDChecklist.Client.Shared.Models;

namespace ADHDChecklist.Client.Features.Dashboard.Services
{
    public interface ITaskService
    {
        Task<List<TaskResponse>> GetTasksByDateAsync(DateOnly date);
        Task<TaskResponse?> GetTaskByIdAsync(Guid id);
        Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request);
        Task<TaskResponse> UpdateTaskAsync(Guid id, UpdateTaskRequest request);
        Task<bool> DeleteTaskAsync(Guid id);
        Task<bool> ToggleTaskCompletionAsync(Guid id);
        Task<bool> MoveTaskToTimeSlotAsync(Guid id, TimeOnly start, TimeOnly end);
        Task<bool> MoveTaskToInboxAsync(Guid id);
        Task<bool> UpdateTaskOrderAsync(Guid id, int newIndex);
        
        // New methods
        Task<int> AutoAdjustTasksAsync();
        Task<int> GetOverdueCountAsync();
    }
}

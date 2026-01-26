using ADHDChecklist.Client.Infrastructure.Services;
using ADHDChecklist.Client.Shared.Models;
namespace ADHDChecklist.Client.Features.Dashboard.Services
{
    public interface ITaskService
    {
        Task<List<TaskResponse>> GetTasksByDateAsync(DateOnly date);
        Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request);
        Task<TaskResponse> UpdateTaskAsync(Guid id, UpdateTaskRequest request);
        Task<bool> DeleteTaskAsync(Guid id);
        Task<bool> ToggleTaskCompletionAsync(Guid id);
    }

    public class TaskService : ITaskService
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<TaskService> _logger;

        public TaskService(IApiClient apiClient, ILogger<TaskService> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task<List<TaskResponse>> GetTasksByDateAsync(DateOnly date)
        {
            try
            {
                var response = await _apiClient.GetAsync<TaskListResponse>(
                    $"/api/tasks?date={date:yyyy-MM-dd}"
                );
                return response?.Tasks ?? new List<TaskResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks");
                return new List<TaskResponse>();
            }
        }

        public async Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request)
        {
            var response = await _apiClient.PostAsync<TaskResponse>("/api/tasks", request);
            return response!;
        }

        public async Task<TaskResponse> UpdateTaskAsync(Guid id, UpdateTaskRequest request)
        {
            var response = await _apiClient.PutAsync<TaskResponse>($"/api/tasks/{id}", request);
            return response!;
        }

        public async Task<bool> DeleteTaskAsync(Guid id)
        {
            try
            {
                await _apiClient.DeleteAsync<object>($"/api/tasks/{id}");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ToggleTaskCompletionAsync(Guid id)
        {
            try
            {
                await _apiClient.PostAsync<object>($"/api/tasks/{id}/toggle-completion");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

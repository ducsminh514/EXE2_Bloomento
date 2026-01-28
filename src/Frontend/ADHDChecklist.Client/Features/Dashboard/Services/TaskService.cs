using ADHDChecklist.Client.Infrastructure.Services;
using ADHDChecklist.Client.Shared.Models;
namespace ADHDChecklist.Client.Features.Dashboard.Services
{
 
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
        public async Task<TaskResponse?> GetTaskByIdAsync(Guid id)
        {
            try
            {
                return await _apiClient.GetAsync<TaskResponse>($"/api/tasks/{id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task by id");
                return null;
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
        public async Task<bool> MoveTaskToTimeSlotAsync(Guid id, TimeOnly start, TimeOnly end)
        {
            try
            {
                await _apiClient.PostAsync<object>(
                    $"/api/tasks/{id}/move-to-timeslot",
                    new { TimeBlockStart = start.ToString("HH:mm"), TimeBlockEnd = end.ToString("HH:mm") }
                );
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> MoveTaskToInboxAsync(Guid id)
        {
            try
            {
                await _apiClient.PostAsync<object>($"/api/tasks/{id}/move-to-inbox");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateTaskOrderAsync(Guid id, int orderIndex)
        {
            try
            {
                await _apiClient.PostAsync<object>(
                    $"/api/tasks/{id}/order",
                    new { OrderIndex = orderIndex }
                );
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> AutoAdjustTasksAsync()
        {
            try
            {
                // api/tasks/auto-adjust returns int
                return await _apiClient.PostAsync<int>("/api/tasks/auto-adjust");
            }
            catch
            {
                return 0;
            }
        }

        public async Task<int> GetOverdueCountAsync()
        {
            try
            {
                return await _apiClient.GetAsync<int>("/api/tasks/overdue/count");
            }
            catch
            {
                return 0;
            }
        }

        public async Task<string[]> BreakdownTaskAsync(string taskTitle)
        {
            try
            {
                var response = await _apiClient.PostAsync<BreakdownTaskResponse>(
                    "/api/ai/breakdown",
                    new { TaskTitle = taskTitle }
                );
                return response?.Steps ?? Array.Empty<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error breaking down task");
                return Array.Empty<string>();
            }
        }
    }

    public record BreakdownTaskResponse(string[] Steps);
}

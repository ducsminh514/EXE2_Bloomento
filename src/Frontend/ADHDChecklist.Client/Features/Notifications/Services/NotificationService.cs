using System.Net.Http.Json;
using ADHDChecklist.Client.Shared.Models;
using ADHDChecklist.Client.Infrastructure.Services;

namespace ADHDChecklist.Client.Features.Notifications.Services;

public interface INotificationService
{
    Task<List<NotificationResponse>> GetNotificationsAsync();
    Task MarkAsReadAsync(Guid id);
    event Action? OnChange;
}

public class NotificationService : INotificationService
{
    private readonly IApiClient _apiClient;
    public event Action? OnChange;

    public NotificationService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<List<NotificationResponse>> GetNotificationsAsync()
    {
        try
        {
            var result = await _apiClient.GetAsync<List<NotificationResponse>>("api/notifications");
            return result ?? new List<NotificationResponse>();
        }
        catch
        {
            return new List<NotificationResponse>();
        }
    }

    public async Task MarkAsReadAsync(Guid id)
    {
        try 
        {
            await _apiClient.PutAsync<object>($"api/notifications/{id}/read", null!);
            NotifyStateChanged();
        }
        catch {}
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}

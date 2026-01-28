using ADHDChecklist.Client.Shared.Models;
using ADHDChecklist.Client.Infrastructure.Services;
using System.Net.Http.Json;

namespace ADHDChecklist.Client.Features.Dashboard.Services;

public interface IBrainDumpService
{
    Task<List<BrainDumpItemResponse>> GetItemsAsync();
    Task<BrainDumpItemResponse?> CreateItemAsync(string content);
    Task<bool> DeleteItemAsync(Guid id);
}

public class BrainDumpService : IBrainDumpService
{
    private readonly IApiClient _apiClient;

    public BrainDumpService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<List<BrainDumpItemResponse>> GetItemsAsync()
    {
        var response = await _apiClient.GetAsync<ApiResponse<List<BrainDumpItemResponse>>>("/api/braindump");
        return response?.Data ?? new List<BrainDumpItemResponse>();
    }

    public async Task<BrainDumpItemResponse?> CreateItemAsync(string content)
    {
        var request = new CreateBrainDumpItemRequest(content);
        var response = await _apiClient.PostAsync<ApiResponse<BrainDumpItemResponse>>("/api/braindump", request);
        return response?.Data;
    }

    public async Task<bool> DeleteItemAsync(Guid id)
    {
        var response = await _apiClient.DeleteAsync<ApiResponse<object>>($"/api/braindump/{id}");
        return response?.Success ?? false;
    }
}

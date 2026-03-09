using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Blazored.LocalStorage;
using ADHDChecklist.Client.Features.Auth.Services; // Add this

namespace ADHDChecklist.Client.Infrastructure.Services;

public interface IApiClient
{
    Task<T?> GetAsync<T>(string endpoint);
    Task<T?> PostAsync<T>(string endpoint, object? data = null);
    Task<T?> PostFileAsync<T>(string endpoint, MultipartFormDataContent content);
    Task<T?> PutAsync<T>(string endpoint, object data);
    Task PatchAsync(string endpoint, object? data = null);
    Task DeleteAsync(string endpoint);
    Task<T?> DeleteAsync<T>(string endpoint);
    void SetAuthToken(string token);
    void ClearAuthToken();
}

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<ApiClient> _logger;
    private readonly IServiceProvider _serviceProvider; // Use ServiceProvider to avoid circular dependency
    private static SemaphoreSlim _refreshSemaphore = new SemaphoreSlim(1, 1);

    public ApiClient(
        HttpClient httpClient,
        ILocalStorageService localStorage,
        ILogger<ApiClient> logger,
        IServiceProvider serviceProvider)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    private IAuthService AuthService => (IAuthService)_serviceProvider.GetService(typeof(IAuthService))!;

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.GetAsync(endpoint);
            
            if (response.StatusCode == HttpStatusCode.Unauthorized) return response;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GET request failed: {Endpoint}, Status: {Status}", endpoint, response.StatusCode);
                return response;
            }

            return response;
        }, async (response) => 
        {
            if (response.StatusCode == HttpStatusCode.NoContent) return default;
            return await response.Content.ReadFromJsonAsync<T>();
        });
    }

    public async Task<T?> PostAsync<T>(string endpoint, object? data = null)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.PostAsJsonAsync(endpoint, data);
            if (response.StatusCode == HttpStatusCode.Unauthorized) return response;
            return response;
        }, async (response) => 
        {
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(error, null, response.StatusCode);
            }
            if (response.StatusCode == HttpStatusCode.NoContent) return default;
            var contentStr = await response.Content.ReadAsStringAsync();
            return string.IsNullOrWhiteSpace(contentStr) ? default : JsonSerializer.Deserialize<T>(contentStr, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        });
    }

    public async Task<T?> PostFileAsync<T>(string endpoint, MultipartFormDataContent content)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            await SetAuthHeaderAsync();
            return await _httpClient.PostAsync(endpoint, content);
        }, async (response) => 
        {
            if (!response.IsSuccessStatusCode) return default;
            if (response.StatusCode == HttpStatusCode.NoContent) return default;
            return await response.Content.ReadFromJsonAsync<T>();
        });
    }

    public async Task PatchAsync(string endpoint, object? data = null)
    {
        await ExecuteWithRetryAsync<object>(async () =>
        {
            await SetAuthHeaderAsync();
            return await _httpClient.PatchAsJsonAsync(endpoint, data);
        }, async (response) => 
        {
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("PATCH request failed: {Endpoint}, Status: {Status}, Error: {Error}", endpoint, response.StatusCode, error);
            }
            return null;
        });
    }

    public async Task<T?> PutAsync<T>(string endpoint, object data)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            await SetAuthHeaderAsync();
            return await _httpClient.PutAsJsonAsync(endpoint, data);
        }, async (response) => 
        {
            if (!response.IsSuccessStatusCode) return default;
            if (response.StatusCode == HttpStatusCode.NoContent) return default;
            var contentStr = await response.Content.ReadAsStringAsync();
            return string.IsNullOrWhiteSpace(contentStr) ? default : JsonSerializer.Deserialize<T>(contentStr, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        });
    }

    public async Task DeleteAsync(string endpoint)
    {
        await ExecuteWithRetryAsync<object>(async () =>
        {
            await SetAuthHeaderAsync();
            return await _httpClient.DeleteAsync(endpoint);
        }, async (response) => 
        {
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(error, null, response.StatusCode);
            }
            return null;
        });
    }

    public async Task<T?> DeleteAsync<T>(string endpoint)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            await SetAuthHeaderAsync();
            return await _httpClient.DeleteAsync(endpoint);
        }, async (response) => 
        {
            if (!response.IsSuccessStatusCode) return default;
            if (response.StatusCode == HttpStatusCode.NoContent) return default;
            var contentStr = await response.Content.ReadAsStringAsync();
            return string.IsNullOrWhiteSpace(contentStr) ? default : JsonSerializer.Deserialize<T>(contentStr, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        });
    }

    private async Task<T?> ExecuteWithRetryAsync<T>(Func<Task<HttpResponseMessage>> action, Func<HttpResponseMessage, Task<T?>> processResult)
    {
        var response = await action();

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogInformation("401 Unauthorized detected. Attempting token refresh...");
            
            bool refreshed = false;
            await _refreshSemaphore.WaitAsync();
            try
            {
                // Re-check token in case another thread already refreshed it
                var currentToken = await _localStorage.GetItemAsync<string>("accessToken");
                // If the token in header is different from current storage, someone else refreshed it
                var headerToken = _httpClient.DefaultRequestHeaders.Authorization?.Parameter;
                
                if (currentToken != headerToken && !string.IsNullOrEmpty(currentToken))
                {
                    refreshed = true;
                }
                else
                {
                    refreshed = await AuthService.RefreshTokenAsync();
                }
            }
            finally
            {
                _refreshSemaphore.Release();
            }

            if (refreshed)
            {
                _logger.LogInformation("Token refresh successful. Retrying request...");
                response = await action(); // Retry
            }
            else
            {
                _logger.LogWarning("Token refresh failed. Redirecting to logout.");
                await AuthService.LogoutAsync();
                // Optionally throw or return default
                return default;
            }
        }

        return await processResult(response);
    }

    public void SetAuthToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    public void ClearAuthToken()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    private async Task SetAuthHeaderAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("accessToken");
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }
}

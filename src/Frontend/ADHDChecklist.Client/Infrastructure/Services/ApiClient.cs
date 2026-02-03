using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Blazored.LocalStorage;

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

    public ApiClient(
        HttpClient httpClient,
        ILocalStorageService localStorage,
        ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        try
        {
            await SetAuthHeaderAsync();

            var response = await _httpClient.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GET request failed: {Endpoint}, Status: {Status}",
                    endpoint, response.StatusCode);
                return default;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return default;
            }

            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GET request to {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<T?> PostAsync<T>(string endpoint, object? data = null)
    {
        try
        {
            await SetAuthHeaderAsync();

            var response = await _httpClient.PostAsJsonAsync(endpoint, data);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("POST request failed: {Endpoint}, Status: {Status}, Error: {Error}",
                    endpoint, response.StatusCode, error);
                
                throw new HttpRequestException(error, null, response.StatusCode);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return default;
            }
            
             var contentStr = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(contentStr))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(contentStr, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "HTTP Error in POST request to {Endpoint}. Inner: {InnerMessage}",
                endpoint, httpEx.InnerException?.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in POST request to {Endpoint}. Type: {ExceptionType}",
                endpoint, ex.GetType().Name);
            throw;
        }
    }

    public async Task<T?> PostFileAsync<T>(string endpoint, MultipartFormDataContent content)
    {
        try
        {
            await SetAuthHeaderAsync();

            var response = await _httpClient.PostAsync(endpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("POST FILE request failed: {Endpoint}, Status: {Status}, Error: {Error}",
                    endpoint, response.StatusCode, error);
                return default;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return default;
            }

            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in POST FILE request to {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task PatchAsync(string endpoint, object? data = null)
    {
        try
        {
            await SetAuthHeaderAsync();

            var response = await _httpClient.PatchAsJsonAsync(endpoint, data);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("PATCH request failed: {Endpoint}, Status: {Status}, Error: {Error}",
                    endpoint, response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PATCH request to {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<T?> PutAsync<T>(string endpoint, object data)
    {
        try
        {
            await SetAuthHeaderAsync();

            var response = await _httpClient.PutAsJsonAsync(endpoint, data);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("PUT request failed: {Endpoint}, Status: {Status}",
                    endpoint, response.StatusCode);
                return default;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return default;
            }

            // Check for empty body on 200 OK
            if (response.Content.Headers.ContentLength == 0)
            {
                 return default;
            }
            
            // Or peek content string (safer if ContentLength is missing)
            var contentStr = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(contentStr))
            {
                return default;
            }

            // Rewind or re-create content? ReadAsStringAsync consumes stream? 
            // Better: use try-catch or just check length if reliable.
            // Safe approach: Deserialize string.
            return JsonSerializer.Deserialize<T>(contentStr, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PUT request to {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task DeleteAsync(string endpoint)
    {
        try
        {
            await SetAuthHeaderAsync();

            var response = await _httpClient.DeleteAsync(endpoint);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("DELETE request failed: {Endpoint}, Status: {Status}, Error: {Error}",
                    endpoint, response.StatusCode, error);
                
                throw new HttpRequestException(error, null, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DELETE request to {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<T?> DeleteAsync<T>(string endpoint)
    {
        try
        {
            await SetAuthHeaderAsync();

            var response = await _httpClient.DeleteAsync(endpoint);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("DELETE request failed: {Endpoint}, Status: {Status}",
                    endpoint, response.StatusCode);
                return default;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return default;
            }

            // Check for empty body on 200 OK
            if (response.Content.Headers.ContentLength == 0)
            {
                 return default;
            }
             var contentStr = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(contentStr))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(contentStr, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DELETE request to {Endpoint}", endpoint);
            throw;
        }
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
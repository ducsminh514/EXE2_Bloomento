using ADHDChecklist.Client.Infrastructure.Services;
using ADHDChecklist.Client.Features.Admin.DTOs;

namespace ADHDChecklist.Client.Features.Admin.Services;

public interface IAdminService
{
    Task<AdminDashboardStats?> GetDashboardStatsAsync();
    Task<AdminDashboardChartsResponse?> GetDashboardChartsAsync();
    Task<UserListResponse?> GetUsersAsync(int page = 1, int pageSize = 10, string? search = null);
    Task<UserStatsDto?> GetUserStatsAsync();
    Task<UserDetailResponse?> GetUserDetailAsync(string userId);
    Task<bool> ToggleUserLockAsync(string userId);
}

public class AdminService : IAdminService
{
    private readonly IApiClient _apiClient;

    public AdminService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<AdminDashboardStats?> GetDashboardStatsAsync()
    {
        try
        {
            return await _apiClient.GetAsync<AdminDashboardStats>("/api/admin/dashboard/stats");
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<AdminDashboardChartsResponse?> GetDashboardChartsAsync()
    {
        try
        {
            return await _apiClient.GetAsync<AdminDashboardChartsResponse>("/api/admin/dashboard/charts");
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<UserListResponse?> GetUsersAsync(int page = 1, int pageSize = 10, string? search = null)
    {
        try
        {
            var url = $"/api/admin/users?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(search))
            {
                url += $"&search={Uri.EscapeDataString(search)}";
            }
            return await _apiClient.GetAsync<UserListResponse>(url);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<UserStatsDto?> GetUserStatsAsync()
    {
        try
        {
            return await _apiClient.GetAsync<UserStatsDto>("/api/admin/users/stats");
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<UserDetailResponse?> GetUserDetailAsync(string userId)
    {
        try
        {
            return await _apiClient.GetAsync<UserDetailResponse>($"/api/admin/users/{userId}");
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<bool> ToggleUserLockAsync(string userId)
    {
        try
        {
            await _apiClient.PatchAsync($"/api/admin/users/{userId}/lock", null);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

public class AdminDashboardStats
{
    public int TotalUsers { get; set; }
    public int NewUsersToday { get; set; }
    public int TotalArticles { get; set; }
    public int TotalComments { get; set; }
    public int HiddenComments { get; set; }
    public int TotalCategories { get; set; }
}

public class AdminDashboardChartsResponse
{
    public List<UserGrowthData> UserGrowth { get; set; } = new();
    public List<CategoryDistributionData> CategoryDistribution { get; set; } = new();
}

public class UserGrowthData
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

public class CategoryDistributionData
{
    public string CategoryName { get; set; } = string.Empty;
    public int ArticleCount { get; set; }
}

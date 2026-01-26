using ADHDChecklist.Client.Infrastructure.Services;
using ADHDChecklist.Client.Shared.Models;

namespace ADHDChecklist.Client.Features.Dashboard.Services
{
    public interface ICategoryService
    {
        Task<List<CategoryResponse>> GetCategoriesAsync();
        Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request);
        Task<CategoryResponse> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request);
        Task<bool> DeleteCategoryAsync(Guid id);
    }

    public class CategoryService : ICategoryService
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(IApiClient apiClient, ILogger<CategoryService> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task<List<CategoryResponse>> GetCategoriesAsync()
        {
            try
            {
                var response = await _apiClient.GetAsync<List<CategoryResponse>>("/api/categories");
                return response ?? new List<CategoryResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return new List<CategoryResponse>();
            }
        }

        public async Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request)
        {
            var response = await _apiClient.PostAsync<CategoryResponse>("/api/categories", request);
            return response!;
        }

        public async Task<CategoryResponse> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request)
        {
            var response = await _apiClient.PutAsync<CategoryResponse>($"/api/categories/{id}", request);
            return response!;
        }

        public async Task<bool> DeleteCategoryAsync(Guid id)
        {
            try
            {
                await _apiClient.DeleteAsync<object>($"/api/categories/{id}");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

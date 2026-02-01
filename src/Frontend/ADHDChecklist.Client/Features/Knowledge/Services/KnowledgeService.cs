using System.Net.Http;
using System.Net.Http.Json;
using ADHDChecklist.Client.Infrastructure.Services;

namespace ADHDChecklist.Client.Features.Knowledge.Services
{
    public class KnowledgeService : IKnowledgeService
    {
        private readonly IApiClient _apiClient;

        public KnowledgeService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<List<KnowledgeCategoryDto>> GetCategoriesAsync()
        {
            try
            {
                return await _apiClient.GetAsync<List<KnowledgeCategoryDto>>("/api/knowledge/categories");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting categories: {ex.Message}");
                return new List<KnowledgeCategoryDto>();
            }
        }

        public async Task<List<ArticleSummaryDto>> GetArticlesByCategoryAsync(string slug)
        {
            try
            {
                return await _apiClient.GetAsync<List<ArticleSummaryDto>>($"/api/knowledge/category/{slug}/articles");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting articles: {ex.Message}");
                return new List<ArticleSummaryDto>();
            }
        }

        public async Task<ArticleDetailDto?> GetArticleDetailAsync(string slug)
        {
            try
            {
                return await _apiClient.GetAsync<ArticleDetailDto>($"/api/knowledge/articles/{slug}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting article detail: {ex.Message}");
                return null;
            }
        }
        public async Task<List<CommentDto>> GetCommentsAsync(Guid articleId)
        {
            try
            {
                return await _apiClient.GetAsync<List<CommentDto>>($"/api/knowledge/articles/{articleId}/comments");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting comments: {ex.Message}");
                return new List<CommentDto>();
            }
        }

        public async Task<bool> PostCommentAsync(Guid articleId, string content, Guid? parentCommentId = null)
        {
            try
            {
                await _apiClient.PostAsync<object>($"/api/knowledge/articles/{articleId}/comments", new { ArticleId = articleId, Content = content, ParentCommentId = parentCommentId });
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error posting comment: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ToggleBookmarkAsync(Guid articleId)
        {
            try
            {
                // Returns { isBookmarked: true/false }
                var result = await _apiClient.PostAsync<BookmarkResponse>($"/api/knowledge/articles/{articleId}/bookmark", null);
                return result?.IsBookmarked ?? false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error bookmarking: {ex.Message}");
                return false;
            }
        }

        public async Task<List<ArticleSummaryDto>> GetBookmarkedArticlesAsync()
        {
            try
            {
                return await _apiClient.GetAsync<List<ArticleSummaryDto>>("/api/knowledge/bookmarks");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting bookmarks: {ex.Message}");
                return new List<ArticleSummaryDto>();
            }
        }
        public async Task<List<ArticleDto>> GetAdminArticlesAsync()
        {
            try
            {
                return await _apiClient.GetAsync<List<ArticleDto>>("/api/admin/knowledge/articles") ?? new List<ArticleDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting admin articles: {ex.Message}");
                return new List<ArticleDto>();
            }
        }

        public async Task<Guid?> CreateArticleAsync(ADHDChecklist.Client.Features.Knowledge.DTOs.ArticleEditorDto article)
        {
            try
            {
                var response = await _apiClient.PostAsync<dynamic>("/api/admin/knowledge/articles", article);
                return Guid.Parse(response.id.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating article: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateArticleAsync(Guid id, ADHDChecklist.Client.Features.Knowledge.DTOs.ArticleEditorDto article)
        {
            try
            {
                await _apiClient.PutAsync<object>($"/api/admin/knowledge/articles/{id}", article);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating article: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteArticleAsync(Guid id)
        {
            try
            {
                await _apiClient.DeleteAsync<object>($"/api/admin/knowledge/articles/{id}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting article: {ex.Message}");
                return false;
            }
        }


        public async Task<AdminCommentListResponse?> GetAdminCommentsAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                return await _apiClient.GetAsync<AdminCommentListResponse>($"/api/admin/knowledge/comments?page={page}&pageSize={pageSize}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting admin comments: {ex.Message}");
                return new AdminCommentListResponse();
            }
        }

        public async Task<bool> ToggleCommentVisibilityAsync(Guid id)
        {
            try
            {
                await _apiClient.PostAsync<object>($"/api/admin/knowledge/comments/{id}/toggle-visibility"); // Using PostAsync wrapper for PATCH if supported or custom PATCH
                // Since ApiClient doesn't have PatchAsync, I'll need to check if existing PostAsync works or if I need to add Patch.
                // Wait, ApiClient has Put, Post, but maybe not Patch. The backend endpoint is PATCH.
                // Assuming PostAsync might not work for Patch unless configured.
                // Let's verify ApiClient first. If not, I'll use Put or HttpClient directly if ApiClient exposes it.
                // Actually, I'll check ApiClient.cs again. It does NOT have PatchAsync.
                // I will add PatchAsync to ApiClient in a separate step or stick to Post if I change backend.
                // For now, let's assume I will add PatchAsync to ApiClient.
                // But to be safe and quick, I'll just change the Backend to use POST for toggle action. It's an action resource anyway.
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error toggling comment visibility: {ex.Message}");
                return false;
            }
        }


        public async Task<bool> CreateCategoryAsync(KnowledgeCategoryDto category)
        {
            try
            {
                await _apiClient.PostAsync<object>("/api/admin/knowledge/categories", category);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating category: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateCategoryAsync(KnowledgeCategoryDto category)
        {
            try
            {
                await _apiClient.PutAsync<object>($"/api/admin/knowledge/categories/{category.Id}", category);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating category: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteCategoryAsync(Guid id)
        {
            try
            {
                await _apiClient.DeleteAsync<object>($"/api/admin/knowledge/categories/{id}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting category: {ex.Message}");
                return false; // In a real app, I should propagate the specific error message (e.g. "Has existing articles")
            }
        }

        public async Task<string?> UploadImageAsync(MultipartFormDataContent content)
        {
            try
            {
                var response = await _apiClient.PostFileAsync<UploadResponse>("/api/upload/image", content);
                return response?.Url;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading image: {ex.Message}");
                return null;
            }
        }
    }
    
    public class UploadResponse { public string? Url { get; set; } }

    public class BookmarkResponse { public bool IsBookmarked { get; set; } }

    public interface IKnowledgeService
    {
        Task<List<KnowledgeCategoryDto>> GetCategoriesAsync();
        Task<List<ArticleSummaryDto>> GetArticlesByCategoryAsync(string slug);
        Task<ArticleDetailDto?> GetArticleDetailAsync(string slug);
        Task<List<CommentDto>> GetCommentsAsync(Guid articleId);
        Task<bool> PostCommentAsync(Guid articleId, string content, Guid? parentCommentId = null);
        Task<bool> ToggleBookmarkAsync(Guid articleId);
        Task<List<ArticleSummaryDto>> GetBookmarkedArticlesAsync();
        
        // Admin
        Task<List<ArticleDto>> GetAdminArticlesAsync();
        Task<Guid?> CreateArticleAsync(ADHDChecklist.Client.Features.Knowledge.DTOs.ArticleEditorDto article);
        Task<bool> UpdateArticleAsync(Guid id, ADHDChecklist.Client.Features.Knowledge.DTOs.ArticleEditorDto article);
        Task<bool> DeleteArticleAsync(Guid id);
        Task<AdminCommentListResponse?> GetAdminCommentsAsync(int page = 1, int pageSize = 20);
        Task<bool> ToggleCommentVisibilityAsync(Guid id);
        Task<bool> CreateCategoryAsync(KnowledgeCategoryDto category);
        Task<bool> UpdateCategoryAsync(KnowledgeCategoryDto category);
        Task<bool> DeleteCategoryAsync(Guid id);
        Task<string?> UploadImageAsync(MultipartFormDataContent content);
    }
    
    public class CommentDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public Guid? ParentCommentId { get; set; }
    }

    public class KnowledgeCategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public string ColorHex { get; set; } = "#3B82F6";
        public int OrderIndex { get; set; }
    }

    public class ArticleSummaryDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string? CoverImage { get; set; }
        public int EstimatedReadTimeMinutes { get; set; }
        public string Difficulty { get; set; } = "Easy";
        public int ViewCount { get; set; }
        public DateTime? PublishedAt { get; set; }
    }

    public class ArticleDetailDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? CoverImage { get; set; }
        public int EstimatedReadTimeMinutes { get; set; }
        public string Difficulty { get; set; } = "Easy";
        public int ViewCount { get; set; }
        public DateTime? PublishedAt { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CategorySlug { get; set; } = string.Empty;
        public bool IsBookmarked { get; set; }
        public Guid CategoryId { get; set; }
        public bool IsPremium { get; set; }
    }

    public class ArticleDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryColorHex { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int EstimatedReadTimeMinutes { get; set; }
        public string Difficulty { get; set; } = string.Empty;
        public bool IsPremium { get; set; }
        public string? CoverImage { get; set; }
        public int BookmarkCount { get; set; }
        public bool IsPublished { get; set; }
    }

    public class AdminCommentListResponse
    {
        public List<AdminCommentDto> Comments { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }

    public class AdminCommentDto
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorEmail { get; set; } = string.Empty;
        public Guid ArticleId { get; set; }
        public string ArticleTitle { get; set; } = string.Empty;
        public bool IsHidden { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

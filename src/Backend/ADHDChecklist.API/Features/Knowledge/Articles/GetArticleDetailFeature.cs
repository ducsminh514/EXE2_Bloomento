using ADHDChecklist.API.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using ADHDChecklist.API.Entities;

namespace ADHDChecklist.API.Features.Knowledge.Articles
{
    // DTO
    public record ArticleDetailDto(
        Guid Id,
        string Title,
        string Slug,
        string? Summary,
        string Content,
        string? CoverImage,
        int EstimatedReadTimeMinutes,
        string Difficulty,
        int ViewCount,
        DateTime? PublishedAt,
        string AuthorName,
        string CategoryName,
        string CategorySlug,
        bool IsBookmarked,
        Guid CategoryId,
        bool IsPremium
    );

    // Query
    public record GetArticleDetailQuery(string Slug) : IRequest<ArticleDetailDto?>;

    // Handler
    public class GetArticleDetailHandler : IRequestHandler<GetArticleDetailQuery, ArticleDetailDto?>
    {
        private readonly AppDbContext _context;

        private readonly IHttpContextAccessor _httpContextAccessor;

        public GetArticleDetailHandler(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ArticleDetailDto?> Handle(GetArticleDetailQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Articles
                .Include(a => a.Category)
                .Include(a => a.Author)
                .AsQueryable();

            var user = _httpContextAccessor.HttpContext?.User;
            var isAdmin = user?.IsInRole("Admin") ?? false;

            if (!isAdmin)
            {
                query = query.Where(a => a.IsPublished);
            }

            var article = await query.FirstOrDefaultAsync(a => a.Slug == request.Slug, cancellationToken);

            if (article == null) return null;

            // Check if bookmarked
            var isBookmarked = false;
            var userIdString = user?.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out var userId))
            {
                isBookmarked = await _context.ArticleBookmarks
                    .AnyAsync(b => b.ArticleId == article.Id && b.UserId == userId, cancellationToken);
            }

            // Increment view count (simple implementation, improved later)
            article.ViewCount++;
            await _context.SaveChangesAsync(cancellationToken);

            return new ArticleDetailDto(
                article.Id,
                article.Title,
                article.Slug,
                article.Summary,
                article.Content,
                article.CoverImage,
                article.EstimatedReadTimeMinutes,
                article.Difficulty,
                article.ViewCount,
                article.PublishedAt,
                article.Author?.UserName ?? "Bloomento Team",
                article.Category?.Name ?? "General",
                article.Category?.Slug ?? "",
                isBookmarked,
                article.CategoryId ?? Guid.Empty,
                article.IsPremium
            );
        }
    }

    // Endpoint Extension (will be added to ArticleEndpoints)
    public static class ArticleDetailEndpoint
    {
        public static void MapArticleDetailEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/knowledge/articles/{slug}", async (string slug, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetArticleDetailQuery(slug));
                if (result == null) return Results.NotFound();
                return Results.Ok(result);
            })
            .WithTags("Knowledge")
            .AllowAnonymous();
        }
    }
}

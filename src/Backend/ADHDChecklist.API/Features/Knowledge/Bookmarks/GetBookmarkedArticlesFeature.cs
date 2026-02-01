using System.Security.Claims;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Features.Knowledge.Articles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Features.Knowledge.Bookmarks
{
    // Query
    public record GetBookmarkedArticlesQuery : IRequest<List<ArticleSummaryDto>>;

    // Handler
    public class GetBookmarkedArticlesHandler : IRequestHandler<GetBookmarkedArticlesQuery, List<ArticleSummaryDto>>
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GetBookmarkedArticlesHandler(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<ArticleSummaryDto>> Handle(GetBookmarkedArticlesQuery request, CancellationToken cancellationToken)
        {
            var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                throw new UnauthorizedAccessException("User not found");
            }

            return await _context.ArticleBookmarks
                .Include(b => b.Article)
                .Where(b => b.UserId == userId && b.Article.IsPublished)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new ArticleSummaryDto(
                    b.Article.Id,
                    b.Article.Title,
                    b.Article.Slug,
                    b.Article.Summary,
                    b.Article.CoverImage,
                    b.Article.EstimatedReadTimeMinutes,
                    b.Article.Difficulty,
                    b.Article.ViewCount,
                    b.Article.PublishedAt
                ))
                .ToListAsync(cancellationToken);
        }
    }

    // Endpoint
    public static class GetBookmarkedArticlesEndpoint
    {
        public static void MapGetBookmarkedArticlesEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/knowledge/bookmarks", async (IMediator mediator) =>
            {
                var result = await mediator.Send(new GetBookmarkedArticlesQuery());
                return Results.Ok(result);
            })
            .WithTags("Knowledge")
            .RequireAuthorization();
        }
    }
}

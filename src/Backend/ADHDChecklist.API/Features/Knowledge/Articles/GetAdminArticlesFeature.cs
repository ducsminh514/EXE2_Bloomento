using ADHDChecklist.API.Data;
using ADHDChecklist.API.Features.Knowledge.Articles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Features.Knowledge.Articles
{
    // Query
    public record GetAdminArticlesQuery : IRequest<List<ArticleDto>>;

    // Handler
    public class GetAdminArticlesHandler : IRequestHandler<GetAdminArticlesQuery, List<ArticleDto>>
    {
        private readonly AppDbContext _context;
        private readonly ILogger<GetAdminArticlesHandler> _logger;

        public GetAdminArticlesHandler(AppDbContext context, ILogger<GetAdminArticlesHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<ArticleDto>> Handle(GetAdminArticlesQuery request, CancellationToken cancellationToken)
        {
            try
            {
            var articles = await _context.Articles
                .Include(a => a.Category)
                .Include(a => a.Author) // Include Author to avoid null access (if Author is required in entity but good to be safe)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new ArticleDto(
                    a.Id,
                    a.Title,
                    a.Slug,
                    a.Summary,
                    a.Category != null ? a.Category.Name : "Uncategorized",
                    a.Category != null ? a.Category.ColorHex : "#cccccc",
                    a.Author != null ? (a.Author.FullName ?? a.Author.UserName ?? "Admin") : "Unknown",
                    a.CreatedAt,
                    a.EstimatedReadTimeMinutes,
                    a.Difficulty,
                    a.IsPremium,
                    a.CoverImage,
                    a.BookmarkCount,
                    a.IsPublished
                ))
                .ToListAsync(cancellationToken);

                return articles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin articles");
                throw; // Rethrow to let global handler handle it (returns 500 but now logged)
            }
        }
    }

    // DTO
    public record ArticleDto(
        Guid Id,
        string Title,
        string Slug,
        string? Summary,
        string CategoryName,
        string CategoryColorHex,
        string AuthorName,
        DateTime CreatedAt,
        int EstimatedReadTimeMinutes,
        string Difficulty,
        bool IsPremium,
        string? CoverImage,
        int BookmarkCount,
        bool IsPublished
    );

    // Endpoint
    public static class GetAdminArticlesEndpoint
    {
        public static void MapGetAdminArticlesEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/admin/knowledge/articles", async (IMediator mediator) =>
            {
                var articles = await mediator.Send(new GetAdminArticlesQuery());
                return Results.Ok(articles);
            })
            .WithTags("Admin - Knowledge")
            .RequireAuthorization("AdminPolicy");
        }
    }
}

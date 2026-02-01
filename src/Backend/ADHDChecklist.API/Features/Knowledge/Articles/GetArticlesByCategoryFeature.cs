using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Features.Knowledge.Articles
{
    // DTO
    public record ArticleSummaryDto(
        Guid Id, 
        string Title, 
        string Slug, 
        string? Summary, 
        string? CoverImage,
        int EstimatedReadTimeMinutes,
        string Difficulty,
        int ViewCount,
        DateTime? PublishedAt
    );

    // Query
    public record GetArticlesByCategoryQuery(string CategorySlug) : IRequest<List<ArticleSummaryDto>>;

    // Handler
    public class GetArticlesByCategoryHandler : IRequestHandler<GetArticlesByCategoryQuery, List<ArticleSummaryDto>>
    {
        private readonly AppDbContext _context;

        public GetArticlesByCategoryHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ArticleSummaryDto>> Handle(GetArticlesByCategoryQuery request, CancellationToken cancellationToken)
        {
            return await _context.Articles
                .Include(a => a.Category)
                .Where(a => a.Category!.Slug == request.CategorySlug && a.IsPublished)
                .OrderByDescending(a => a.PublishedAt)
                .Select(a => new ArticleSummaryDto(
                    a.Id,
                    a.Title,
                    a.Slug,
                    a.Summary,
                    a.CoverImage,
                    a.EstimatedReadTimeMinutes,
                    a.Difficulty,
                    a.ViewCount,
                    a.PublishedAt
                ))
                .ToListAsync(cancellationToken);
        }
    }

    // Endpoint
    public static class ArticleEndpoints
    {
        public static void MapArticleEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/knowledge/category/{slug}/articles", async (string slug, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetArticlesByCategoryQuery(slug));
                return Results.Ok(result);
            })
            .WithTags("Knowledge")
            .AllowAnonymous();
        }
    }
}

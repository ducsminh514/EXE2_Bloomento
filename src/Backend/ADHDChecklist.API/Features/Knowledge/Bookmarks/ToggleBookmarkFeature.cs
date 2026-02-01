using System.Security.Claims;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Features.Knowledge.Bookmarks
{
    // Command
    public record ToggleBookmarkCommand(Guid ArticleId) : IRequest<bool>; // Returns true if bookmarked, false if removed

    // Handler
    public class ToggleBookmarkHandler : IRequestHandler<ToggleBookmarkCommand, bool>
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ToggleBookmarkHandler(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> Handle(ToggleBookmarkCommand request, CancellationToken cancellationToken)
        {
            var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                throw new UnauthorizedAccessException("User not found");
            }

            var existingBookmark = await _context.ArticleBookmarks
                .FirstOrDefaultAsync(b => b.ArticleId == request.ArticleId && b.UserId == userId, cancellationToken);

            var article = await _context.Articles.FindAsync(new object[] { request.ArticleId }, cancellationToken);
            if (article == null) throw new KeyNotFoundException("Article not found");

            if (existingBookmark != null)
            {
                // Remove bookmark
                _context.ArticleBookmarks.Remove(existingBookmark);
                article.BookmarkCount = Math.Max(0, article.BookmarkCount - 1);
                await _context.SaveChangesAsync(cancellationToken);
                return false; // Not bookmarked anymore
            }
            else
            {
                // Add bookmark
                var bookmark = new ArticleBookmark
                {
                    ArticleId = request.ArticleId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ArticleBookmarks.Add(bookmark);
                article.BookmarkCount++;
                await _context.SaveChangesAsync(cancellationToken);
                return true; // Bookmarked
            }
        }
    }

    // Endpoint
    public static class ToggleBookmarkEndpoint
    {
        public static void MapToggleBookmarkEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/knowledge/articles/{articleId:guid}/bookmark", async (Guid articleId, IMediator mediator) =>
            {
                var isBookmarked = await mediator.Send(new ToggleBookmarkCommand(articleId));
                return Results.Ok(new { IsBookmarked = isBookmarked });
            })
            .WithTags("Knowledge")
            .RequireAuthorization();
        }
    }
}

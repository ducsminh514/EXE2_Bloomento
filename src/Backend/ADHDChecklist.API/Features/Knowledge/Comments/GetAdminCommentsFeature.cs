using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Features.Knowledge.Comments
{
    // Query
    public record GetAdminCommentsQuery(int Page = 1, int PageSize = 20, string? Status = null) : IRequest<AdminCommentListResponse>;

    // Response DTOs
    public record AdminCommentListResponse(List<AdminCommentDto> Comments, int TotalCount, int CurrentPage, int TotalPages);
    
    public record AdminCommentDto(
        Guid Id,
        string Content,
        string AuthorName,
        string AuthorEmail, // Useful for admin to identify user
        Guid ArticleId,
        string ArticleTitle,
        bool IsHidden,
        DateTime CreatedAt
    );

    // Handler
    public class GetAdminCommentsHandler : IRequestHandler<GetAdminCommentsQuery, AdminCommentListResponse>
    {
        private readonly AppDbContext _context;

        public GetAdminCommentsHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AdminCommentListResponse> Handle(GetAdminCommentsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.ArticleComments
                .Include(c => c.User)
                .Include(c => c.Article)
                .AsNoTracking();

            if (!string.IsNullOrEmpty(request.Status))
            {
                if (request.Status.Equals("hidden", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(c => c.IsHidden);
                }
                else if (request.Status.Equals("visible", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(c => !c.IsHidden);
                }
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            // If empty after filter (and page 1), result is empty
            if (totalCount == 0)
            {
                 return new AdminCommentListResponse(new List<AdminCommentDto>(), 0, request.Page, 0);
            }

            var comments = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(c => new AdminCommentDto(
                    c.Id,
                    c.Content,
                    c.User.FullName ?? "Unknown",
                    c.User.Email ?? "No Email",
                    c.ArticleId,
                    c.Article.Title,
                    c.IsHidden,
                    c.CreatedAt
                ))
                .ToListAsync(cancellationToken);

            return new AdminCommentListResponse(comments, totalCount, request.Page, totalPages);
        }
    }

    // Endpoint
    public static class GetAdminCommentsEndpoint
    {
        public static void MapGetAdminCommentsEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/admin/knowledge/comments", async (int? page, int? pageSize, string? status, IMediator mediator) =>
            {
                var query = new GetAdminCommentsQuery(page ?? 1, pageSize ?? 20, status);
                var result = await mediator.Send(query);
                return Results.Ok(result);
            })
            .WithTags("Admin - Knowledge")
            .RequireAuthorization("AdminPolicy");
        }
    }
}

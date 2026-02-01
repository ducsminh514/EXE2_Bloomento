using ADHDChecklist.API.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Features.Knowledge.Comments
{
    // DTO
    public record CommentDto(
        Guid Id,
        Guid UserId,
        string UserName,
        string Content,
        DateTime CreatedAt,
        bool IsAuthor,
        Guid? ParentCommentId
    );

    // Query
    public record GetCommentsQuery(Guid ArticleId) : IRequest<List<CommentDto>>;

    // Handler
    public class GetCommentsHandler : IRequestHandler<GetCommentsQuery, List<CommentDto>>
    {
        private readonly AppDbContext _context;

        public GetCommentsHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<CommentDto>> Handle(GetCommentsQuery request, CancellationToken cancellationToken)
        {
            // Note: For MVP we fetch all comments. For prod, pagination is needed.
            return await _context.ArticleComments
                .Include(c => c.User)
                .Where(c => c.ArticleId == request.ArticleId && !c.IsHidden)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentDto(
                    c.Id,
                    c.UserId,
                    c.User.UserName ?? "Anonymous",
                    c.Content,
                    c.CreatedAt,
                    false, // IsAuthor logic requires current user context, omitted for simple list
                    c.ParentCommentId
                ))
                .ToListAsync(cancellationToken);
        }
    }

    // Endpoint
    public static class GetCommentsEndpoint
    {
        public static void MapGetCommentsEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/knowledge/articles/{articleId:guid}/comments", async (Guid articleId, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetCommentsQuery(articleId));
                return Results.Ok(result);
            })
            .WithTags("Knowledge")
            .AllowAnonymous();
        }
    }
}

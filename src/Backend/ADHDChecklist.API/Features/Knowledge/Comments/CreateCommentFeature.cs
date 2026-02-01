using System.Security.Claims;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Features.Knowledge.Comments
{
    // Command
    public record CreateCommentCommand(Guid ArticleId, string Content, Guid? ParentCommentId = null) : IRequest<Guid>;

    // Validator
    public class CreateCommentValidator : AbstractValidator<CreateCommentCommand>
    {
        public CreateCommentValidator()
        {
            RuleFor(x => x.ArticleId).NotEmpty();
            RuleFor(x => x.Content).NotEmpty().MaximumLength(1000);
        }
    }

    // Handler
    public class CreateCommentHandler : IRequestHandler<CreateCommentCommand, Guid>
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CreateCommentHandler(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Guid> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
        {
            var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                throw new UnauthorizedAccessException("User not found");
            }

            var articleExists = await _context.Articles.AnyAsync(a => a.Id == request.ArticleId, cancellationToken);
            if (!articleExists)
            {
                throw new KeyNotFoundException("Article not found");
            }

            var comment = new ArticleComment
            {
                ArticleId = request.ArticleId,
                UserId = userId,
                Content = request.Content,
                ParentCommentId = request.ParentCommentId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ArticleComments.Add(comment);
            
            // Increment Comment Counter on Article (Simple approach)
            var article = await _context.Articles.FindAsync(new object[] { request.ArticleId }, cancellationToken);
            if (article != null)
            {
                article.CommentCount++;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return comment.Id;
        }
    }

    // Endpoint
    public static class CreateCommentEndpoint
    {
        public static void MapCreateCommentEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/knowledge/articles/{articleId:guid}/comments", async (Guid articleId, CreateCommentCommand command, IMediator mediator) =>
            {
                if (articleId != command.ArticleId) return Results.BadRequest("ID Mismatch");
                
                var commentId = await mediator.Send(command);
                return Results.Created($"/api/knowledge/comments/{commentId}", new { Id = commentId });
            })
            .WithTags("Knowledge")
            .RequireAuthorization();
        }
    }
}

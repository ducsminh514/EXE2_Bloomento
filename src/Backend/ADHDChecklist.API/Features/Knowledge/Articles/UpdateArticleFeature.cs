using ADHDChecklist.API.Data;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Features.Knowledge.Articles
{
    // Command
    public record UpdateArticleCommand(
        Guid Id,
        string Title,
        string Slug,
        string? Summary,
        string Content,
        string? CoverImage,
        Guid CategoryId,
        int EstimatedReadTimeMinutes,
        string Difficulty,
        bool IsPublished,
        bool IsPremium
    ) : IRequest<bool>;

    // Validator
    public class UpdateArticleValidator : AbstractValidator<UpdateArticleCommand>
    {
        public UpdateArticleValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Slug).NotEmpty().MaximumLength(200).Matches("^[a-z0-9-]+$");
            RuleFor(x => x.Content).NotEmpty();
            RuleFor(x => x.CategoryId).NotEmpty();
        }
    }

    // Handler
    public class UpdateArticleHandler : IRequestHandler<UpdateArticleCommand, bool>
    {
        private readonly AppDbContext _context;

        public UpdateArticleHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(UpdateArticleCommand request, CancellationToken cancellationToken)
        {
            var article = await _context.Articles.FindAsync(new object[] { request.Id }, cancellationToken);
            if (article == null) return false;

            // Check slug uniqueness if changed
            if (article.Slug != request.Slug)
            {
                if (await _context.Articles.AnyAsync(a => a.Slug == request.Slug, cancellationToken))
                {
                    throw new InvalidOperationException("Slug already exists");
                }
            }

            article.Title = request.Title;
            article.Slug = request.Slug;
            article.Summary = request.Summary;
            article.Content = request.Content;
            article.CoverImage = request.CoverImage;
            article.CategoryId = request.CategoryId;
            article.EstimatedReadTimeMinutes = request.EstimatedReadTimeMinutes;
            article.Difficulty = request.Difficulty;
            article.IsPublished = request.IsPublished;
            article.IsPremium = request.IsPremium;
            article.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    // Endpoint
    public static class UpdateArticleEndpoint
    {
        public static void MapUpdateArticleEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapPut("/api/admin/knowledge/articles/{id:guid}", async (Guid id, UpdateArticleCommand command, IMediator mediator) =>
            {
                if (id != command.Id) return Results.BadRequest("ID Mismatch");
                var success = await mediator.Send(command);
                return success ? Results.Ok() : Results.NotFound();
            })
            .WithTags("Admin - Knowledge")
            .RequireAuthorization("AdminPolicy");
        }
    }
}

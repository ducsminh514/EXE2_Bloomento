using System.Security.Claims;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Features.Knowledge.Articles
{
    // Command
    public record CreateArticleCommand(
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
    ) : IRequest<Guid>;

    // Validator
    public class CreateArticleValidator : AbstractValidator<CreateArticleCommand>
    {
        public CreateArticleValidator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Slug).NotEmpty().MaximumLength(200).Matches("^[a-z0-9-]+$").WithMessage("Slug must be kebab-case");
            RuleFor(x => x.Content).NotEmpty();
            RuleFor(x => x.CategoryId).NotEmpty();
            RuleFor(x => x.Difficulty).Must(d => new[] { "Easy", "Medium", "Hard" }.Contains(d));
        }
    }

    // Handler
    public class CreateArticleHandler : IRequestHandler<CreateArticleCommand, Guid>
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CreateArticleHandler(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Guid> Handle(CreateArticleCommand request, CancellationToken cancellationToken)
        {
            var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                throw new UnauthorizedAccessException("User not found");
            }

            // Check if slug exists
            if (await _context.Articles.AnyAsync(a => a.Slug == request.Slug, cancellationToken))
            {
                throw new InvalidOperationException("Slug already exists");
            }

            var article = new Article
            {
                Title = request.Title,
                Slug = request.Slug,
                Summary = request.Summary,
                Content = request.Content,
                CoverImage = request.CoverImage,
                CategoryId = request.CategoryId,
                AuthorId = userId,
                EstimatedReadTimeMinutes = request.EstimatedReadTimeMinutes,
                Difficulty = request.Difficulty,
                IsPublished = request.IsPublished,
                IsPremium = request.IsPremium,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Articles.Add(article);
            await _context.SaveChangesAsync(cancellationToken);

            return article.Id;
        }
    }

    // Endpoint
    public static class CreateArticleEndpoint
    {
        public static void MapCreateArticleEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/admin/knowledge/articles", async (CreateArticleCommand command, IMediator mediator) =>
            {
                var articleId = await mediator.Send(command);
                return Results.Created($"/api/knowledge/articles/{articleId}", new { Id = articleId });
            })
            .WithTags("Admin - Knowledge")
            .RequireAuthorization("AdminPolicy"); // Define policy later
        }
    }
}

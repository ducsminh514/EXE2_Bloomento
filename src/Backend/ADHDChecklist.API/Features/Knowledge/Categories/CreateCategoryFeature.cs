using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using MediatR;
using FluentValidation;

namespace ADHDChecklist.API.Features.Knowledge.Categories
{
    // Command
    public record CreateCategoryCommand(string Name, string? Description, string? Icon, string ColorHex, int OrderIndex) : IRequest<Guid>;

    // Validator
    public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
    {
        public CreateCategoryCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ColorHex).NotEmpty().Matches("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$");
        }
    }

    // Handler
    public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, Guid>
    {
        private readonly AppDbContext _context;

        public CreateCategoryHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = new KnowledgeCategory
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Slug = request.Name.ToLower().Replace(" ", "-").Replace(",", ""), // Simple slug generation
                Description = request.Description,
                Icon = request.Icon,
                ColorHex = request.ColorHex,
                OrderIndex = request.OrderIndex
            };

            _context.KnowledgeCategories.Add(category);
            await _context.SaveChangesAsync(cancellationToken);

            return category.Id;
        }
    }

    // Endpoint
    public static class CreateCategoryEndpoint
    {
        public static void MapCreateCategoryEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/admin/knowledge/categories", async (CreateCategoryCommand command, IMediator mediator) =>
            {
                var id = await mediator.Send(command);
                return Results.Created($"/api/admin/knowledge/categories/{id}", new { Id = id });
            })
            .WithTags("Admin - Knowledge")
            .RequireAuthorization("AdminPolicy");
        }
    }
}

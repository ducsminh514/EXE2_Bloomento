using ADHDChecklist.API.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using FluentValidation;

namespace ADHDChecklist.API.Features.Knowledge.Categories
{
    // Command
    public record UpdateCategoryCommand(Guid Id, string Name, string? Description, string? Icon, string ColorHex, int OrderIndex) : IRequest<bool>;

    // Validator
    public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
    {
        public UpdateCategoryCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ColorHex).NotEmpty().Matches("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$");
        }
    }

    // Handler
    public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, bool>
    {
        private readonly AppDbContext _context;

        public UpdateCategoryHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _context.KnowledgeCategories.FindAsync(new object[] { request.Id }, cancellationToken);

            if (category == null)
            {
                return false;
            }

            category.Name = request.Name;
            category.Slug = request.Name.ToLower().Replace(" ", "-").Replace(",", "");
            category.Description = request.Description;
            category.Icon = request.Icon;
            category.ColorHex = request.ColorHex;
            category.OrderIndex = request.OrderIndex;

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    // Endpoint
    public static class UpdateCategoryEndpoint
    {
        public static void MapUpdateCategoryEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapPut("/api/admin/knowledge/categories/{id}", async (Guid id, UpdateCategoryCommand command, IMediator mediator) =>
            {
                if (id != command.Id) return Results.BadRequest();
                var result = await mediator.Send(command);
                return result ? Results.Ok() : Results.NotFound();
            })
            .WithTags("Admin - Knowledge")
            .RequireAuthorization("AdminPolicy");
        }
    }
}

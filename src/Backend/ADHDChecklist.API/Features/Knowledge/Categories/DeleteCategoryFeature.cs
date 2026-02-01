using ADHDChecklist.API.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Features.Knowledge.Categories
{
    // Command
    public record DeleteCategoryCommand(Guid Id) : IRequest<bool>;

    // Handler
    public class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand, bool>
    {
        private readonly AppDbContext _context;

        public DeleteCategoryHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
        {
            // Check for articles
            var hasArticles = await _context.Articles.AnyAsync(a => a.CategoryId == request.Id, cancellationToken);
            if (hasArticles)
            {
                throw new InvalidOperationException("Cannot delete category because it has associated articles. Please move or delete the articles first.");
            }

            var category = await _context.KnowledgeCategories.FindAsync(new object[] { request.Id }, cancellationToken);

            if (category == null)
            {
                return false;
            }

            _context.KnowledgeCategories.Remove(category);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }

    // Endpoint
    public static class DeleteCategoryEndpoint
    {
        public static void MapDeleteCategoryEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapDelete("/api/admin/knowledge/categories/{id}", async (Guid id, IMediator mediator) =>
            {
                try
                {
                    var result = await mediator.Send(new DeleteCategoryCommand(id));
                    return result ? Results.Ok() : Results.NotFound();
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new { Message = ex.Message });
                }
            })
            .WithTags("Admin - Knowledge")
            .RequireAuthorization("AdminPolicy");
        }
    }
}

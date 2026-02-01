using ADHDChecklist.API.Data;
using MediatR;

namespace ADHDChecklist.API.Features.Knowledge.Articles
{
    // Command
    public record DeleteArticleCommand(Guid Id) : IRequest<bool>;

    // Handler
    public class DeleteArticleHandler : IRequestHandler<DeleteArticleCommand, bool>
    {
        private readonly AppDbContext _context;

        public DeleteArticleHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(DeleteArticleCommand request, CancellationToken cancellationToken)
        {
            var article = await _context.Articles.FindAsync(new object[] { request.Id }, cancellationToken);
            if (article == null) return false;

            _context.Articles.Remove(article);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    // Endpoint
    public static class DeleteArticleEndpoint
    {
        public static void MapDeleteArticleEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapDelete("/api/admin/knowledge/articles/{id:guid}", async (Guid id, IMediator mediator) =>
            {
                var success = await mediator.Send(new DeleteArticleCommand(id));
                return success ? Results.Ok() : Results.NotFound();
            })
            .WithTags("Admin - Knowledge")
            .RequireAuthorization("AdminPolicy");
        }
    }
}

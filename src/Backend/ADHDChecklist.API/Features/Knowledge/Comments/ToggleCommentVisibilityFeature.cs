using ADHDChecklist.API.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Features.Knowledge.Comments
{
    // Command
    public record ToggleCommentVisibilityCommand(Guid Id) : IRequest<bool>;

    // Handler
    public class ToggleCommentVisibilityHandler : IRequestHandler<ToggleCommentVisibilityCommand, bool>
    {
        private readonly AppDbContext _context;

        public ToggleCommentVisibilityHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(ToggleCommentVisibilityCommand request, CancellationToken cancellationToken)
        {
            var comment = await _context.ArticleComments.FindAsync(new object[] { request.Id }, cancellationToken);

            if (comment == null)
            {
                return false;
            }

            comment.IsHidden = !comment.IsHidden;
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }

    // Endpoint
    public static class ToggleCommentVisibilityEndpoint
    {
        public static void MapToggleCommentVisibilityEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/admin/knowledge/comments/{id}/toggle-visibility", async (Guid id, IMediator mediator) =>
            {
                var result = await mediator.Send(new ToggleCommentVisibilityCommand(id));
                return result ? Results.Ok(new { Message = "Comment visibility toggled" }) : Results.NotFound();
            })
            .WithTags("Admin - Knowledge")
            .RequireAuthorization("AdminPolicy");
        }
    }
}

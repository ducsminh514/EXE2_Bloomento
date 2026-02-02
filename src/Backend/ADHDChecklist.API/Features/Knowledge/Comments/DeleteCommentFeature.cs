using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace ADHDChecklist.API.Features.Knowledge.Comments;

public record DeleteCommentCommand(Guid Id) : IRequest<bool>;

public class DeleteCommentHandler : IRequestHandler<DeleteCommentCommand, bool>
{
    private readonly AppDbContext _context;

    public DeleteCommentHandler(AppDbContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task<bool> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch the target comment
        var comment = await _context.ArticleComments
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (comment == null)
        {
            return false;
        }

        // 2. Load all descendants recursively
        var allCommentsToDelete = new List<ArticleComment> { comment };
        await LoadDescendantsAsync(comment, allCommentsToDelete, cancellationToken);

        // 3. Remove all of them
        _context.ArticleComments.RemoveRange(allCommentsToDelete);
        
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async System.Threading.Tasks.Task LoadDescendantsAsync(ArticleComment parent, List<ArticleComment> collector, CancellationToken ct)
    {
        // Load direct children
        await _context.Entry(parent)
            .Collection(c => c.Replies)
            .LoadAsync(ct);

        if (parent.Replies != null && parent.Replies.Any())
        {
            foreach (var child in parent.Replies)
            {
                collector.Add(child);
                await LoadDescendantsAsync(child, collector, ct);
            }
        }
    }
}

public static class DeleteCommentEndpoint
{
    public static void MapDeleteCommentEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/admin/knowledge/comments/{id}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new DeleteCommentCommand(id));
            if (!result)
            {
                return Results.NotFound();
            }
            return Results.Ok();
        })
        .WithTags("Admin - Knowledge")
        .RequireAuthorization("AdminPolicy");
    }
}

using System.Security.Claims;
using ADHDChecklist.API.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Features.BrainDump.DeleteBrainDumpItem;

// Command
public record DeleteBrainDumpItemCommand(
    Guid UserId,
    Guid ItemId
) : IRequest<bool>;

// Handler
public class DeleteBrainDumpItemHandler : IRequestHandler<DeleteBrainDumpItemCommand, bool>
{
    private readonly AppDbContext _context;

    public DeleteBrainDumpItemHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteBrainDumpItemCommand request, CancellationToken ct)
    {
        var item = await _context.BrainDumpItems
            .FirstOrDefaultAsync(x => x.Id == request.ItemId && x.UserId == request.UserId, ct);

        if (item == null) return false;

        _context.BrainDumpItems.Remove(item);
        await _context.SaveChangesAsync(ct);
        return true;
    }
}

// Endpoint
public static class DeleteBrainDumpItemEndpoint
{
    public static void MapDeleteBrainDumpItem(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/braindump/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await mediator.Send(new DeleteBrainDumpItemCommand(userId, id), ct);
            
            return result ? Results.Ok(new { Success = true }) : Results.NotFound(new { Success = false, Message = "Item not found" });
        })
        .RequireAuthorization()
        .WithTags("BrainDump");
    }
}

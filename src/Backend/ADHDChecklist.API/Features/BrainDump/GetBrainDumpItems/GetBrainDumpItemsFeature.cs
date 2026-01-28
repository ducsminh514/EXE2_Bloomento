using System.Security.Claims;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Features.BrainDump.CreateBrainDumpItem; // For Response DTO
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Features.BrainDump.GetBrainDumpItems;

// Query
public record GetBrainDumpItemsQuery(Guid UserId) : IRequest<List<BrainDumpItemResponse>>;

// Handler
public class GetBrainDumpItemsHandler : IRequestHandler<GetBrainDumpItemsQuery, List<BrainDumpItemResponse>>
{
    private readonly AppDbContext _context;

    public GetBrainDumpItemsHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<BrainDumpItemResponse>> Handle(GetBrainDumpItemsQuery request, CancellationToken ct)
    {
        var items = await _context.BrainDumpItems
            .AsNoTracking()
            .Where(x => x.UserId == request.UserId && (x.IsProcessed == false || x.IsProcessed == null))
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new BrainDumpItemResponse(x.Id, x.Content, x.CreatedAt))
            .ToListAsync(ct);

        return items;
    }
}

// Endpoint
public static class GetBrainDumpItemsEndpoint
{
    public static void MapGetBrainDumpItems(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/braindump", async (
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await mediator.Send(new GetBrainDumpItemsQuery(userId), ct);
            return Results.Ok(new { Success = true, Data = result });
        })
        .RequireAuthorization()
        .WithTags("BrainDump");
    }
}

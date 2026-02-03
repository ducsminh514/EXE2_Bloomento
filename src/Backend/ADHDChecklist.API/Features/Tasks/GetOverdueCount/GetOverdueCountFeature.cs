using ADHDChecklist.API.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ADHDChecklist.API.Features.Tasks.GetOverdueCount
{
    // Query
    public record GetOverdueCountQuery(Guid UserId) : IRequest<int>;

    // Handler
    public class GetOverdueCountQueryHandler : IRequestHandler<GetOverdueCountQuery, int>
    {
        private readonly AppDbContext _context;

        public GetOverdueCountQueryHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(GetOverdueCountQuery request, CancellationToken cancellationToken)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return await _context.Tasks
                .CountAsync(t => (t.UserId == request.UserId || t.AssignedUserId == request.UserId) 
                                 && !t.IsCompleted 
                                 && t.ScheduledDate < today, cancellationToken);
        }
    }

    // Endpoint
    public static class GetOverdueCountEndpoint
    {
        public static void MapGetOverdueCount(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/tasks/overdue/count", async (
                ClaimsPrincipal user,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var query = new GetOverdueCountQuery(userId);
                var count = await mediator.Send(query, ct);
                return Results.Ok(count);
            })
            .RequireAuthorization()
            .WithTags("Tasks")
            .WithName("GetOverdueCount")
            .Produces<int>(200);
        }
    }
}

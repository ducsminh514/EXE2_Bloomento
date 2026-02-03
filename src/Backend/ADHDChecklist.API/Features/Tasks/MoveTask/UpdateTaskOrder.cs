using System.Security.Claims;
using ADHDChecklist.API.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace ADHDChecklist.API.Features.Tasks.MoveTask
{
    public record UpdateTaskOrderCommand(
        Guid TaskId,
        int NewOrderIndex,
        Guid UserId
    ) : IRequest<bool>;

    public class UpdateTaskOrderHandler : IRequestHandler<UpdateTaskOrderCommand, bool>
    {
        private readonly AppDbContext _context;

        public UpdateTaskOrderHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(UpdateTaskOrderCommand request, CancellationToken cancellationToken)
        {
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == request.TaskId && (t.UserId == request.UserId || t.AssignedUserId == request.UserId), cancellationToken);

            if (task == null) return false;

            task.OrderIndex = request.NewOrderIndex;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    public static class UpdateTaskOrderEndpoint
    {
        public static void MapUpdateTaskOrder(this IEndpointRouteBuilder app)
        {
            app.MapPatch("/api/tasks/{id:guid}/order", async (
                Guid id,
                UpdateOrderRequest request,
                ClaimsPrincipal user,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var command = new UpdateTaskOrderCommand(id, request.OrderIndex, userId);
                var result = await mediator.Send(command, ct);
                return result ? Results.Ok() : Results.NotFound();
            })
            .RequireAuthorization()
            .WithTags("Tasks")
            .WithName("UpdateTaskOrder");
        }
    }

    public record UpdateOrderRequest(int OrderIndex);
}

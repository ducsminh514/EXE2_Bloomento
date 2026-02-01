using ADHDChecklist.API.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ADHDChecklist.API.Features.Tasks.AutoAdjust
{
    // Command
    public record AutoAdjustTasksCommand(Guid UserId) : IRequest<int>; // Returns count of moved tasks

    // Handler
    public class AutoAdjustTasksCommandHandler : IRequestHandler<AutoAdjustTasksCommand, int>
    {
        private readonly AppDbContext _context;

        public AutoAdjustTasksCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(AutoAdjustTasksCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
            if (user == null || !user.IsPremium())
            {
                 throw new ADHDChecklist.API.Entities.Exceptions.PremiumFeatureException(
                    "Tính năng 'Dời việc quá hạn' chỉ dành cho Premium.", 
                    "Auto-Adjust Tasks");
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var overdueTasks = await _context.Tasks
                .Where(t => t.UserId == request.UserId 
                            && !t.IsCompleted 
                            && t.ScheduledDate < today)
                .ToListAsync(cancellationToken);

            if (!overdueTasks.Any()) return 0;

            foreach (var task in overdueTasks)
            {
                task.ScheduledDate = today;
                task.TimeBlockStart = null; // Reset time so user can re-plan
                task.TimeBlockEnd = null;
                task.RescheduleCount++;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return overdueTasks.Count;
        }
    }

    // Endpoint
    public static class AutoAdjustTasksEndpoint
    {
        public static void MapAutoAdjustTasks(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/tasks/auto-adjust", async (
                ClaimsPrincipal user,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var command = new AutoAdjustTasksCommand(userId);
                var count = await mediator.Send(command, ct);
                return Results.Ok(count);
            })
            .RequireAuthorization()
            .WithTags("Tasks")
            .WithName("AutoAdjustTasks")
            .Produces<int>(200);
        }
    }
}

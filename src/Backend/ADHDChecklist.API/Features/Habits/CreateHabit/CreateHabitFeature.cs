using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using ADHDChecklist.API.Shared.DTOs;
using MediatR;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Features.Habits.CreateHabit
{
    // Command
    public record CreateHabitCommand(
        string Title,
        string? Description,
        string? ColorHex,
        string Frequency,
        Guid UserId
    ) : IRequest<Guid>;

    // Handler
    public class CreateHabitCommandHandler : IRequestHandler<CreateHabitCommand, Guid>
    {
        private readonly AppDbContext _context;

        public CreateHabitCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(CreateHabitCommand request, CancellationToken cancellationToken)
        {
            var familyId = await _context.FamilyMembers
                .Where(fm => fm.UserId == request.UserId)
                .Select(fm => (Guid?)fm.FamilyId)
                .FirstOrDefaultAsync(cancellationToken);

            var habit = new Habit
            {
                Name = request.Title,
                Description = request.Description,
                ColorHex = request.ColorHex,
                Frequency = request.Frequency, // "daily" or "weekly"
                DaysOfWeek = null, // Default null for now
                UserId = request.UserId,
                CreatedAt = DateTime.UtcNow,
                CurrentStreak = 0,
                LongestStreak = 0,
                IsActive = true,
                FamilyId = familyId == Guid.Empty ? null : familyId
            };

            _context.Habits.Add(habit);
            await _context.SaveChangesAsync(cancellationToken);

            return habit.Id;
        }
    }

    // Endpoint
    public static class CreateHabitEndpoint
    {
        public static void MapCreateHabit(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/habits", async (
                CreateHabitRequest request,
                ClaimsPrincipal user,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var command = new CreateHabitCommand(
                    request.Title,
                    request.Description,
                    request.ColorHex,
                    request.Frequency,
                    userId
                );

                var habitId = await mediator.Send(command, ct);

                return Results.Ok(habitId);
            })
            .RequireAuthorization()
            .WithTags("Habits")
            .WithName("CreateHabit")
            .Produces<Guid>(200);
        }
    }
}

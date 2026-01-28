using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using ADHDChecklist.API.Shared.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ADHDChecklist.API.Features.Habits.ToggleHabit
{
    // Command
    public record ToggleHabitCommand(
        Guid HabitId,
        DateOnly Date,
        Guid UserId
    ) : IRequest<bool>; // Returns true if completed, false if incompleted (toggled state)

    // Handler
    public class ToggleHabitCommandHandler : IRequestHandler<ToggleHabitCommand, bool>
    {
        private readonly AppDbContext _context;

        public ToggleHabitCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(ToggleHabitCommand request, CancellationToken cancellationToken)
        {
            var habit = await _context.Habits
                .FirstOrDefaultAsync(h => h.Id == request.HabitId && h.UserId == request.UserId, cancellationToken);
            
            if (habit == null) throw new InvalidOperationException("Habit not found");

            var existingCompletion = await _context.HabitCompletions
                .FirstOrDefaultAsync(hc => hc.HabitId == request.HabitId && hc.CompletionDate == request.Date, cancellationToken);

            bool isNowCompleted;

            if (existingCompletion != null)
            {
                // Uncheck
                _context.HabitCompletions.Remove(existingCompletion);
                isNowCompleted = false;
            }
            else
            {
                // Check
                var completion = new HabitCompletion
                {
                    HabitId = request.HabitId,
                    CompletionDate = request.Date,
                    CompletedAt = DateTime.UtcNow
                };
                _context.HabitCompletions.Add(completion);
                isNowCompleted = true;
            }

            // Recalculate Streaks
            var allCompletions = await _context.HabitCompletions
                .Where(hc => hc.HabitId == request.HabitId)
                .OrderBy(hc => hc.CompletionDate)
                .Select(hc => hc.CompletionDate)
                .ToListAsync(cancellationToken);

            // 1. Current Streak
            int currentStreak = 0;
            var checkDate = DateOnly.FromDateTime(DateTime.UtcNow);
            
            if (habit.Frequency == "weekly")
            {
                // Weekly Logic: Check consecutive weeks
                // Get current week start (Monday)
                var currentWeekStart = checkDate.AddDays(-(int)checkDate.DayOfWeek + 1);
                
                // If today is Sunday (0), DayOfWeek is 0. adjustment needed. 
                // .NET DayOfWeek: Sunday=0. standard ISO: Sunday=7.
                // Re-calc consistently:
                int diff = (7 + (int)checkDate.DayOfWeek - 1) % 7;
                currentWeekStart = checkDate.AddDays(-1 * diff);

                // Check this week
                bool thisWeekDone = allCompletions.Any(d => d >= currentWeekStart && d < currentWeekStart.AddDays(7));
                
                // If this week is NOT done, check last week. If done, start count.
                // However, usually streak includes current period if done? Or just finished periods?
                // Standard: "Current Streak" includes up to now.
                
                DateOnly probeWeek = currentWeekStart;
                if (!thisWeekDone) probeWeek = probeWeek.AddDays(-7); // Start checking from last week
                
                while (true)
                {
                    var weekEnd = probeWeek.AddDays(7);
                    bool hasCompletion = allCompletions.Any(d => d >= probeWeek && d < weekEnd);
                    if (hasCompletion)
                    {
                        currentStreak++;
                        probeWeek = probeWeek.AddDays(-7);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                // Daily Logic
                if (!allCompletions.Contains(checkDate))
                {
                    checkDate = checkDate.AddDays(-1);
                }

                while (allCompletions.Contains(checkDate))
                {
                    currentStreak++;
                    checkDate = checkDate.AddDays(-1);
                }
            }
            habit.CurrentStreak = currentStreak;

            // 2. Longest Streak (Simplified for now - Daily only or simple max)
             habit.LongestStreak = Math.Max(habit.LongestStreak ?? 0, currentStreak); 
             // Note: True longest streak for weekly is harder to calc retrospectively without scanning all history. 
             // For MVP, updating Longest based on Current is acceptable.


            await _context.SaveChangesAsync(cancellationToken);
            return isNowCompleted;
        }
    }

    // Endpoint
    public static class ToggleHabitEndpoint
    {
        public static void MapToggleHabit(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/habits/{id}/toggle", async (
                Guid id,
                ToggleHabitRequest request,
                ClaimsPrincipal user,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var command = new ToggleHabitCommand(id, request.Date, userId);
                var result = await mediator.Send(command, ct);

                return Results.Ok(result);
            })
            .RequireAuthorization()
            .WithTags("Habits")
            .WithName("ToggleHabit")
            .Produces<bool>(200);
        }
    }
}

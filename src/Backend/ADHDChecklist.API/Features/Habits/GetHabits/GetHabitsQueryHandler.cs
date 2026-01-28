using ADHDChecklist.API.Data;
using ADHDChecklist.API.Shared.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ADHDChecklist.API.Features.Habits.GetHabits
{
    public class GetHabitsQueryHandler : IRequestHandler<GetHabitsQuery, List<HabitResponse>>
    {
        private readonly AppDbContext _context;

        public GetHabitsQueryHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<HabitResponse>> Handle(GetHabitsQuery request, CancellationToken cancellationToken)
        {
            // Fetch habits and completions
            // For now, fetch ALL completions to calculate "CompletedDates" (or limit to last 30 days if performance needed)
            // But UI might need 7 days. Let's just return dates.
            
            var habits = await _context.Habits
                .Where(h => h.UserId == request.UserId)
                .Include(h => h.HabitCompletions)
                .OrderBy(h => h.CreatedAt)
                .ToListAsync(cancellationToken);

            var habitResponses = habits.Select(h => new HabitResponse(
                h.Id,
                h.Name,
                h.Description,
                h.ColorHex,
                "icon", // Placeholder or mapping needed if icon is not in entity? Entity check: no icon? Check Entity again.
                h.Frequency,
                h.CurrentStreak ?? 0,
                h.LongestStreak ?? 0,
                h.HabitCompletions.Select(hc => hc.CompletionDate).ToList()
            )).ToList();

            return habitResponses;
        }
    }
}

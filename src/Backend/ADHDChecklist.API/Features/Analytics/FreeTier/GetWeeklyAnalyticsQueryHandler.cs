using ADHDChecklist.API.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace ADHDChecklist.API.Features.Analytics.FreeTier
{
    public class GetWeeklyAnalyticsQueryHandler : IRequestHandler<GetWeeklyAnalyticsQuery, WeeklyAnalyticsResponse>
    {
        private readonly AppDbContext _context;

        public GetWeeklyAnalyticsQueryHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<WeeklyAnalyticsResponse> Handle(GetWeeklyAnalyticsQuery request, CancellationToken cancellationToken)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var sevenDaysAgo = today.AddDays(-6); // Last 7 days including today

            // Get tasks completed in last 7 days
            var completedTasks = await _context.Tasks
                .Where(t => t.UserId == request.UserId
                    && t.IsCompleted
                    && t.CompletedAt.HasValue
                    && DateOnly.FromDateTime(t.CompletedAt.Value) >= sevenDaysAgo
                    && DateOnly.FromDateTime(t.CompletedAt.Value) <= today)
                .GroupBy(t => DateOnly.FromDateTime(t.CompletedAt!.Value))
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            // Get tasks created in last 7 days
            var createdTasks = await _context.Tasks
                .Where(t => t.UserId == request.UserId
                    && DateOnly.FromDateTime(t.CreatedAt) >= sevenDaysAgo
                    && DateOnly.FromDateTime(t.CreatedAt) <= today)
                .GroupBy(t => DateOnly.FromDateTime(t.CreatedAt))
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var dailyStats = new List<DailyStats>();

            for (int i = 0; i < 7; i++)
            {
                var date = sevenDaysAgo.AddDays(i);
                var completed = completedTasks.FirstOrDefault(x => x.Date == date)?.Count ?? 0;
                var created = createdTasks.FirstOrDefault(x => x.Date == date)?.Count ?? 0;

                dailyStats.Add(new DailyStats(
                    date,
                    date.DayOfWeek.ToString(),
                    completed,
                    created
                ));
            }

            var totalCompleted = dailyStats.Sum(d => d.TasksCompleted);
            var totalCreated = dailyStats.Sum(d => d.TasksCreated);
            var completionRate = totalCreated > 0
                ? Math.Round((double)totalCompleted / totalCreated * 100, 1)
                : 0;

            return new WeeklyAnalyticsResponse(
                dailyStats,
                totalCompleted,
                totalCreated,
                completionRate
            );
        }
    }
}

using ADHDChecklist.API.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ADHDChecklist.API.Features.Analytics.Premium
{
    public class GetPremiumAnalyticsQueryHandler : IRequestHandler<GetPremiumAnalyticsQuery, PremiumAnalyticsResponse>
    {
        private readonly AppDbContext _context;

        public GetPremiumAnalyticsQueryHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PremiumAnalyticsResponse> Handle(GetPremiumAnalyticsQuery request, CancellationToken cancellationToken)
        {
            // 1. Time Blindness (Planned vs Actual from FocusSessions)
            // Fetch data first to avoid LINQ translation issues with Sum/Math in Select
            var tasksWithSessions = await _context.Tasks
                .Include(t => t.FocusSessions)
                .Where(t => t.UserId == request.UserId && t.IsCompleted && t.Duration > 0)
                .OrderByDescending(t => t.CompletedAt)
                .Take(10) 
                .ToListAsync(cancellationToken);

            var timeBlindness = tasksWithSessions
                .Select(t => {
                    var actual = t.FocusSessions.Sum(fs => fs.ActualDuration ?? 0);
                    var estimated = t.Duration ?? 0;
                    var deviation = estimated > 0 ? (double)(actual - estimated) / estimated * 100 : 0;
                    return new TimeBlindnessData(t.Title, estimated, actual, deviation);
                })
                .Take(5)
                .ToList();

            // 2. Energy Heatmap (Hourly activity)
            // Fetch completed hours and group in-memory
            var completedHours = await _context.Tasks
                .Where(t => t.UserId == request.UserId && t.IsCompleted && t.CompletedAt.HasValue)
                .Select(t => t.CompletedAt!.Value.Hour)
                .ToListAsync(cancellationToken);

            var energyHeatmap = completedHours
                .GroupBy(h => h)
                .Select(g => new EnergyData(g.Key, g.Count()))
                .OrderBy(e => e.Hour)
                .ToList();

            // 3. Procrastination Debt
            var procrastinationDebt = await _context.Tasks
                .Where(t => t.UserId == request.UserId && !t.IsCompleted && t.RescheduleCount > 0)
                .OrderByDescending(t => t.RescheduleCount)
                .Select(t => new ProcrastinationDebtData(t.Id, t.Title, t.RescheduleCount))
                .Take(5)
                .ToListAsync(cancellationToken);

            // 4. Dopamine Balance
            var dopamineTypes = await _context.Tasks
                .Where(t => t.UserId == request.UserId && t.IsCompleted)
                .Select(t => t.DopamineType)
                .ToListAsync(cancellationToken);

            int lowCount = dopamineTypes.Count(d => d == "Low");
            int highCount = dopamineTypes.Count(d => d == "High");
            double ratio = (lowCount + highCount) > 0 ? (double)highCount / (lowCount + highCount) * 100 : 50;

            var dopamineBalance = new DopamineBalanceData(lowCount, highCount, ratio);

            return new PremiumAnalyticsResponse(
                timeBlindness,
                energyHeatmap,
                procrastinationDebt,
                dopamineBalance
            );
        }
    }
}

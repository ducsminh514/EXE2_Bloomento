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
            var timeBlindness = await _context.Tasks
                .Where(t => t.UserId == request.UserId && t.IsCompleted && t.Duration > 0)
                .Select(t => new TimeBlindnessData(
                    t.Title,
                    t.Duration ?? 0,
                    t.FocusSessions.Sum(fs => fs.ActualDuration ?? 0),
                    t.Duration.HasValue && t.Duration > 0 ? (double)(t.FocusSessions.Sum(fs => fs.ActualDuration ?? 0) - t.Duration.Value) / (double)t.Duration.Value * 100.0 : 0.0
                ))
                .Take(5) // Just top 5 for the chart
                .ToListAsync(cancellationToken);

            // 2. Energy Heatmap (Hourly activity)
            var energyHeatmap = await _context.Tasks
                .Where(t => t.UserId == request.UserId && t.IsCompleted && t.CompletedAt.HasValue)
                .GroupBy(t => t.CompletedAt!.Value.Hour)
                .Select(g => new EnergyData(g.Key, g.Count()))
                .OrderBy(e => e.Hour)
                .ToListAsync(cancellationToken);

            // 3. Procrastination Debt
            var procrastinationDebt = await _context.Tasks
                .Where(t => t.UserId == request.UserId && !t.IsCompleted && t.RescheduleCount > 0)
                .OrderByDescending(t => t.RescheduleCount)
                .Select(t => new ProcrastinationDebtData(t.Id, t.Title, t.RescheduleCount))
                .Take(5)
                .ToListAsync(cancellationToken);

            // 4. Dopamine Balance
            var tasks = await _context.Tasks
                .Where(t => t.UserId == request.UserId && t.IsCompleted)
                .Select(t => t.DopamineType)
                .ToListAsync(cancellationToken);

            int lowCount = tasks.Count(d => d == "Low");
            int highCount = tasks.Count(d => d == "High");
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

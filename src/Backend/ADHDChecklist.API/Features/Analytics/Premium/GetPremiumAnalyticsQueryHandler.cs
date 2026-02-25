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
        // P1-3 FIX: Dùng UTC+7 (Vietnam timezone) thay vì UTC
        private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

        public GetPremiumAnalyticsQueryHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PremiumAnalyticsResponse> Handle(GetPremiumAnalyticsQuery request, CancellationToken cancellationToken)
        {
            // 1. Time Blindness (Planned vs Actual from FocusSessions)
            var tasksWithSessions = await _context.Tasks
                .Include(t => t.FocusSessions)
                .Where(t => t.UserId == request.UserId && t.IsCompleted && t.Duration > 0)
                .OrderByDescending(t => t.CompletedAt)
                .Take(10)
                .ToListAsync(cancellationToken);

            var timeBlindness = tasksWithSessions
                .Select(t => {
                    var actual = t.FocusSessions.Sum(fs => fs.ActualDuration ?? 0) / 60; // Convert seconds to minutes
                    var estimated = t.Duration ?? 0;
                    var deviation = estimated > 0 ? (double)(actual - estimated) / estimated * 100 : 0;
                    return new TimeBlindnessData(t.Title, estimated, actual, deviation);
                })
                .Where(t => t.ActualMinutes > 0) // Chỉ show task có FocusSession data thực
                .Take(5)
                .ToList();

            // 2. Energy Heatmap — P1-3 FIX: convert UTC → UTC+7 trước khi lấy Hour
            var completedAtUtc = await _context.Tasks
                .Where(t => t.UserId == request.UserId && t.IsCompleted && t.CompletedAt.HasValue)
                .Select(t => t.CompletedAt!.Value)
                .ToListAsync(cancellationToken);

            var energyHeatmap = completedAtUtc
                .Select(utc => utc.Add(VietnamOffset).Hour) // UTC → UTC+7
                .GroupBy(h => h)
                .Select(g => new EnergyData(g.Key, g.Count()))
                .OrderBy(e => e.Hour)
                .ToList();

            // 3. Procrastination Debt — chỉ lấy 30 ngày gần đây (tránh nhiễu lịch sử cũ)
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var procrastinationDebt = await _context.Tasks
                .Where(t => t.UserId == request.UserId
                    && !t.IsCompleted
                    && t.RescheduleCount > 0
                    && t.CreatedAt >= thirtyDaysAgo)
                .OrderByDescending(t => t.RescheduleCount)
                .Select(t => new ProcrastinationDebtData(t.Id, t.Title, t.RescheduleCount))
                .Take(5)
                .ToListAsync(cancellationToken);

            // 4. Dopamine Balance — chỉ lấy 30 ngày gần đây
            var dopamineTypes = await _context.Tasks
                .Where(t => t.UserId == request.UserId
                    && t.IsCompleted
                    && t.CompletedAt.HasValue
                    && t.CompletedAt.Value >= thirtyDaysAgo)
                .Select(t => t.DopamineType)
                .ToListAsync(cancellationToken);

            int lowCount = dopamineTypes.Count(d => d == "Low");
            int highCount = dopamineTypes.Count(d => d == "High");
            double ratio = (lowCount + highCount) > 0 ? (double)highCount / (lowCount + highCount) * 100 : 50;
            var dopamineBalance = new DopamineBalanceData(lowCount, highCount, ratio);

            // P1-4: Generate Actionable Advice
            var advice = GenerateAdvice(energyHeatmap, timeBlindness, dopamineBalance);

            return new PremiumAnalyticsResponse(
                timeBlindness,
                energyHeatmap,
                procrastinationDebt,
                dopamineBalance,
                advice
            );
        }

        private static AnalyticsAdvice GenerateAdvice(
            List<EnergyData> heatmap,
            List<TimeBlindnessData> timeBlindness,
            DopamineBalanceData dopamine)
        {
            // Peak hour advice
            string? peakHourAdvice = null;
            if (heatmap.Any())
            {
                var peak = heatmap.OrderByDescending(h => h.CompletedCount).First();
                var hour = peak.Hour;
                var timeStr = hour < 12 ? $"{hour}h sáng" : hour < 18 ? $"{hour - (hour > 12 ? 0 : 0)}h chiều" : $"{hour}h tối";
                peakHourAdvice = $"⚡ Bạn hiệu quả nhất vào lúc {hour}:00 — hãy đặt những task khó vào khung giờ này!";
            }

            // Time blindness advice
            string? timeBlindnessAdvice = null;
            if (timeBlindness.Any(t => t.ActualMinutes > 0))
            {
                var avgDeviation = timeBlindness.Where(t => t.ActualMinutes > 0).Average(t => t.DeviationPercentage);
                if (avgDeviation > 30)
                    timeBlindnessAdvice = $"⏱️ Bạn thường mất nhiều hơn dự kiến {avgDeviation:F0}% — hãy nhân đôi thời gian ước tính khi lên kế hoạch!";
                else if (avgDeviation < -20)
                    timeBlindnessAdvice = "⏱️ Bạn ước tính thời gian khá dư — có thể thử đặt deadline ngắn hơn để tạo áp lực tích cực.";
                else
                    timeBlindnessAdvice = "⏱️ Khả năng ước tính thời gian của bạn khá tốt — tiếp tục phát huy!";
            }

            // Dopamine advice
            string? dopamineAdvice = null;
            if ((dopamine.LowDopamineCount + dopamine.HighDopamineCount) >= 5)
            {
                if (dopamine.BalanceRatio > 70)
                    dopamineAdvice = "🎯 Bạn đang làm nhiều task dễ/thú vị — thử xen kẽ 1 task khó vào mỗi ngày để phát triển hơn.";
                else if (dopamine.BalanceRatio < 30)
                    dopamineAdvice = "💪 Bạn đang cố gắng nhiều việc khó — hãy tự thưởng 1-2 task nhỏ vui trong ngày để duy trì động lực!";
                else
                    dopamineAdvice = "✨ Bạn đang cân bằng tốt giữa task khó và task dễ — tiếp tục duy trì nhé!";
            }

            return new AnalyticsAdvice(peakHourAdvice, timeBlindnessAdvice, dopamineAdvice);
        }
    }
}

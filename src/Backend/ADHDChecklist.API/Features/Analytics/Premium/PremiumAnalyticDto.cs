using System;
using System.Collections.Generic;

namespace ADHDChecklist.API.Features.Analytics.Premium
{
    public record PremiumAnalyticsResponse(
        List<TimeBlindnessData> TimeBlindness,
        List<EnergyData> EnergyHeatmap,
        List<ProcrastinationDebtData> ProcrastinationDebt,
        DopamineBalanceData DopamineBalance
    );

    public record TimeBlindnessData(
        string TaskTitle,
        int EstimatedMinutes,
        int ActualMinutes,
        double DeviationPercentage
    );

    public record EnergyData(
        int Hour,
        int CompletedCount
    );

    public record ProcrastinationDebtData(
        Guid TaskId,
        string Title,
        int RescheduleCount
    );

    public record DopamineBalanceData(
        int LowDopamineCount,
        int HighDopamineCount,
        double BalanceRatio
    );
}

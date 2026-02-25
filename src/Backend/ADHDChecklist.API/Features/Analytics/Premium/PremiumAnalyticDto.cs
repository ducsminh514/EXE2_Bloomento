using System;
using System.Collections.Generic;

namespace ADHDChecklist.API.Features.Analytics.Premium
{
    public record PremiumAnalyticsResponse(
        List<TimeBlindnessData> TimeBlindness,
        List<EnergyData> EnergyHeatmap,
        List<ProcrastinationDebtData> ProcrastinationDebt,
        DopamineBalanceData DopamineBalance,
        AnalyticsAdvice Advice  // P1-4: Actionable advice
    );

    public record TimeBlindnessData(
        string TaskTitle,
        int EstimatedMinutes,
        int ActualMinutes,
        double DeviationPercentage
    );

    // P1-3: Hour là giờ local (UTC+7), không phải UTC nữa
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

    // P1-4: Actionable advice cho từng biểu đồ
    public record AnalyticsAdvice(
        string? PeakHourAdvice,    // "Bạn hiệu quả nhất lúc X giờ → Hãy đặt task khó vào khung này!"
        string? TimeBlindnessAdvice,  // Tóm tắt xu hướng ước tính thời gian
        string? DopamineAdvice     // Gợi ý cân bằng dopamine
    );
}

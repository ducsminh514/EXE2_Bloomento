namespace ADHDChecklist.API.Features.Analytics.FreeTier
{
    public record WeeklyAnalyticsResponse(
        List<DailyStats> DailyStats,
        int TotalCompleted,
        int TotalCreated,
        double CompletionRate
    );

    public record DailyStats(
        DateOnly Date,
        string DayOfWeek,
        int TasksCompleted,
        int TasksCreated
    );
}

using System;
using System.Collections.Generic;

namespace ADHDChecklist.API.Shared.DTOs
{
    public record HabitResponse(
        Guid Id,
        string Title,
        string? Description,
        string? ColorHex,
        string? Icon,
        string Frequency, // "daily", "weekly"
        int CurrentStreak,
        int LongestStreak,
        List<DateOnly> CompletedDates
    );

    public record CreateHabitRequest(
        string Title,
        string? Description,
        string? ColorHex,
        string? Icon,
        string Frequency = "daily"
    );

    public record ToggleHabitRequest(
        DateOnly Date
    );
}

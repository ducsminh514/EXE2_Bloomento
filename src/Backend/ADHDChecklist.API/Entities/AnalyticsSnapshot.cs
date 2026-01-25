using System;
using System.Collections.Generic;
using ADHDChecklist.API.Entities.Common;

namespace ADHDChecklist.API.Entities;

public partial class AnalyticsSnapshot
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public DateOnly SnapshotDate { get; set; }

    public int? TasksCompleted { get; set; }

    public int? TasksCreated { get; set; }

    public int? TotalFocusMinutes { get; set; }

    public int? HabitsCompleted { get; set; }

    public string? HourlyBreakdown { get; set; }

    public string? CategoryBreakdown { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ApplicationUser User { get; set; } = null!;
}

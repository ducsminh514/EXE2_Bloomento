using System;
using System.Collections.Generic;

namespace ADHDChecklist.API.Entities;

public partial class FocusSession
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid? TaskId { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? EndedAt { get; set; }

    public int PlannedDuration { get; set; }

    public int? ActualDuration { get; set; }

    public int? FocusLevel { get; set; }

    public string? WhiteNoiseUsed { get; set; }

    public int? DistractionCount { get; set; }

    public bool? WasCompleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<DistractionLog> DistractionLogs { get; set; } = new List<DistractionLog>();

    public virtual Task? Task { get; set; }

    public virtual User User { get; set; } = null!;
}

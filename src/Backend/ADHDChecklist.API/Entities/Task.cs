using System;
using System.Collections.Generic;

namespace ADHDChecklist.API.Entities;

public partial class Task
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public Guid? CategoryId { get; set; }

    public DateOnly ScheduledDate { get; set; }

    public TimeOnly? TimeBlockStart { get; set; }

    public TimeOnly? TimeBlockEnd { get; set; }

    public int? Duration { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime? CompletedAt { get; set; }

    public int? Priority { get; set; }

    public bool? IsRecurring { get; set; }

    public string? RecurrencePattern { get; set; }

    public Guid? ParentTaskId { get; set; }

    public int? OrderIndex { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<BrainDumpItem> BrainDumpItems { get; set; } = new List<BrainDumpItem>();

    public virtual Category? Category { get; set; }

    public virtual ICollection<FocusSession> FocusSessions { get; set; } = new List<FocusSession>();

    public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();

    public virtual User User { get; set; } = null!;
}

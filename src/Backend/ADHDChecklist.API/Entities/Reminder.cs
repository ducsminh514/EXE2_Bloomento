using System;
using System.Collections.Generic;

namespace ADHDChecklist.API.Entities;

public partial class Reminder
{
    public Guid Id { get; set; }

    public Guid TaskId { get; set; }

    public DateTime RemindAt { get; set; }

    public bool? IsSent { get; set; }

    public DateTime? SentAt { get; set; }

    public string? ReminderType { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Task Task { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace ADHDChecklist.API.Entities;

public partial class HabitCompletion
{
    public Guid Id { get; set; }

    public Guid HabitId { get; set; }

    public DateOnly CompletionDate { get; set; }

    public DateTime CompletedAt { get; set; }

    public string? Notes { get; set; }

    public virtual Habit Habit { get; set; } = null!;
}

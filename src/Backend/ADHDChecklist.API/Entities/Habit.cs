using System;
using System.Collections.Generic;
using ADHDChecklist.API.Entities.Common;

namespace ADHDChecklist.API.Entities;

public partial class Habit
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? ColorHex { get; set; }

    public string Frequency { get; set; } = null!;

    public string? DaysOfWeek { get; set; }

    public int? CurrentStreak { get; set; }

    public int? LongestStreak { get; set; }

    public bool? IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    // Family Features
    public Guid? FamilyId { get; set; }
    public bool IsShared { get; set; } = false;

    public virtual ICollection<HabitCompletion> HabitCompletions { get; set; } = new List<HabitCompletion>();

    public virtual ApplicationUser User { get; set; } = null!;
    public virtual Family? Family { get; set; }
}

using System;
using System.Collections.Generic;

namespace ADHDChecklist.API.Entities;

public partial class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? FullName { get; set; }

    public int SubscriptionTier { get; set; }

    public DateTime? SubscriptionExpiry { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public string? TimeZone { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<AnalyticsSnapshot> AnalyticsSnapshots { get; set; } = new List<AnalyticsSnapshot>();

    public virtual ICollection<BrainDumpItem> BrainDumpItems { get; set; } = new List<BrainDumpItem>();

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

    public virtual ICollection<FocusSession> FocusSessions { get; set; } = new List<FocusSession>();

    public virtual ICollection<Habit> Habits { get; set; } = new List<Habit>();

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();

    public virtual UserPreference? UserPreference { get; set; }
}

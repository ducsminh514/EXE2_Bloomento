using System;
using System.Collections.Generic;

namespace ADHDChecklist.API.Entities;

public partial class UserPreference
{
    public Guid UserId { get; set; }

    public int? ThemeId { get; set; }

    public string? CustomPrimaryColor { get; set; }

    public string? CustomAccentColor { get; set; }

    public int? DefaultTimeBlockDuration { get; set; }

    public bool? AllowFlexibleBlocks { get; set; }

    public int? DefaultFocusLevel { get; set; }

    public string? DefaultWhiteNoise { get; set; }

    public int? DefaultPomodoroWork { get; set; }

    public int? DefaultPomodoroBreak { get; set; }

    public bool? EnableReminders { get; set; }

    public int? ReminderLeadTime { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}

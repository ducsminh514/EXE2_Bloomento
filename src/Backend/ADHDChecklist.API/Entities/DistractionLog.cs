using System;
using System.Collections.Generic;

namespace ADHDChecklist.API.Entities;

public partial class DistractionLog
{
    public Guid Id { get; set; }

    public Guid FocusSessionId { get; set; }

    public DateTime LoggedAt { get; set; }

    public string? DistractionType { get; set; }

    public string? Notes { get; set; }

    public virtual FocusSession FocusSession { get; set; } = null!;
}

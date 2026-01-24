using System;
using System.Collections.Generic;

namespace ADHDChecklist.API.Entities;

public partial class BrainDumpItem
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Content { get; set; } = null!;

    public bool? IsProcessed { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public Guid? ConvertedToTaskId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Task? ConvertedToTask { get; set; }

    public virtual User User { get; set; } = null!;
}

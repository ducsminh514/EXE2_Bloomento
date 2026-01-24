using System;
using System.Collections.Generic;

namespace ADHDChecklist.API.Entities;

public partial class Category
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; } = null!;

    public string? ColorHex { get; set; }

    public string? Icon { get; set; }

    public int? OrderIndex { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();

    public virtual User User { get; set; } = null!;
}

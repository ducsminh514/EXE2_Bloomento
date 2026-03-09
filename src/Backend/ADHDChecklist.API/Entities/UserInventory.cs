using System;
using ADHDChecklist.API.Entities.Common;

namespace ADHDChecklist.API.Entities;

/// <summary>
/// Kho đồ và kinh tế của người dùng liên quan đến hệ thống Linh vật.
/// </summary>
public class UserInventory
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public long Coins { get; set; } = 0;
    public int ResurrectionPotionCount { get; set; } = 0;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual ApplicationUser User { get; set; } = null!;
}

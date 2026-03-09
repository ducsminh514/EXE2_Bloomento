using System;
using ADHDChecklist.API.Entities.Common;

namespace ADHDChecklist.API.Entities.Common;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public string? CreatedByIp { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    
    // Grace period to handle cross-tab race conditions
    public DateTime? GracePeriodExpiresAt { get; set; }
    
    // Concurrency protection
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;

    // Navigation property
    public virtual ApplicationUser User { get; set; } = null!;
}

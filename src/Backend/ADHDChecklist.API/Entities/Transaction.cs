using System;
using ADHDChecklist.API.Entities.Common;

namespace ADHDChecklist.API.Entities;

public class Transaction : IAuditable
{
    public Guid Id { get; set; }
    public long OrderCode { get; set; } // Required by PayOS
    public int Amount { get; set; }
    public string Description { get; set; } = null!;
    public string Status { get; set; } = "PENDING"; // PENDING, PAID, CANCELLED
    
    public Guid UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;
    
    public SubscriptionTier SubscriptionTier { get; set; }
    
    public string? PaymentLinkId { get; set; } // ID from PayOS
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

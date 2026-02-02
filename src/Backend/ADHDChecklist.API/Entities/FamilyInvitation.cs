using System;

namespace ADHDChecklist.API.Entities;

public class FamilyInvitation
{
    public Guid Id { get; set; }
    
    public Guid FamilyId { get; set; }
    
    public string Email { get; set; } = null!;
    
    public string Code { get; set; } = null!;
    
    // Pending, Accepted, Expired, Declined
    public string Status { get; set; } = "Pending";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ExpiresAt { get; set; }

    public virtual Family Family { get; set; } = null!;
}

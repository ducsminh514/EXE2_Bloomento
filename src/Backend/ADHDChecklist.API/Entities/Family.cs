using System;
using System.Collections.Generic;
using ADHDChecklist.API.Entities.Common;

namespace ADHDChecklist.API.Entities;

public class Family : IAuditable
{
    public Guid Id { get; set; }
    
    public string Name { get; set; } = null!;
    
    public Guid OwnerId { get; set; }
    
    // 0: Free, 1: Premium, 2: Family
    public int SubscriptionPlan { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public virtual ApplicationUser Owner { get; set; } = null!;
    public virtual ICollection<FamilyMember> Members { get; set; } = new List<FamilyMember>();
    public virtual ICollection<FamilyInvitation> Invitations { get; set; } = new List<FamilyInvitation>();
}

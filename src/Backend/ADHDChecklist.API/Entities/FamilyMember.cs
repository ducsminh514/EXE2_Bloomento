using System;
using ADHDChecklist.API.Entities.Common;

namespace ADHDChecklist.API.Entities;

public class FamilyMember
{
    public Guid Id { get; set; }
    
    public Guid FamilyId { get; set; }
    
    public Guid UserId { get; set; }
    
    // Role: "Admin", "Member", "Child"
    public string Role { get; set; } = "Member";
    
    public string? Nickname { get; set; }
    
    public DateTime JoinedAt { get; set; }

    // Navigation properties
    public virtual Family Family { get; set; } = null!;
    public virtual ApplicationUser User { get; set; } = null!;
}

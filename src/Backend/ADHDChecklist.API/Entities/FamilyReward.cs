using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ADHDChecklist.API.Entities.Common;
namespace ADHDChecklist.API.Entities;

public class FamilyReward
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid FamilyId { get; set; }

    [ForeignKey("FamilyId")]
    public virtual Family Family { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public int CostPoints { get; set; }

    public bool IsAvailable { get; set; } = true;

    // "PendingApproval", "Active", "Rejected"
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Active";

    public Guid? CreatorId { get; set; }
    public virtual ApplicationUser? Creator { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? RejectionReason { get; set; }
}

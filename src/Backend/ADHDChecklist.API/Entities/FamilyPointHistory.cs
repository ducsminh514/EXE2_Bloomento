using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ADHDChecklist.API.Entities.Common;

namespace ADHDChecklist.API.Entities;

public class FamilyPointHistory
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid FamilyId { get; set; }

    [ForeignKey("FamilyId")]
    public virtual Family Family { get; set; } = null!;

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual ApplicationUser User { get; set; } = null!;

    [Required]
    public int Amount { get; set; }

    [Required]
    [MaxLength(50)]
    public string Source { get; set; } = string.Empty; // "Task", "Habit", "Redemption"

    public Guid? ReferenceId { get; set; } // TaskId or RewardId

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

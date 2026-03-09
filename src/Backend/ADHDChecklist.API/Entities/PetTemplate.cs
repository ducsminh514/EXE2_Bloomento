using System;
using System.Collections.Generic;

namespace ADHDChecklist.API.Entities;

/// <summary>
/// Thể xác: Định nghĩa mẫu linh vật gốc (ví dụ: Mẫu cây tinh linh, Mẫu gấu túi).
/// </summary>
public class PetTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Species { get; set; } = null!; // "Plant" (Option A) hoặc "Beast" (Option B)
    public string Description { get; set; } = null!;
    public string CreatedBy { get; set; } = "System";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual ICollection<PetEvolutionStage> EvolutionStages { get; set; } = new List<PetEvolutionStage>();
    public virtual ICollection<UserPet> UserPets { get; set; } = new List<UserPet>();
}

using System;

namespace ADHDChecklist.API.Entities;

/// <summary>
/// Các giai đoạn tiến hóa của một mẫu linh vật (Level 1, 11, 31).
/// </summary>
public class PetEvolutionStage
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public int RequiredLevel { get; set; }
    public string EvolutionName { get; set; } = null!; // Ví dụ: "Hạt giống", "Mầm xanh"
    public string AssetUrl { get; set; } = null!; // Link file .glb nén Draco

    // Navigation
    public virtual PetTemplate Template { get; set; } = null!;
}

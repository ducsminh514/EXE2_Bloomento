using System;

namespace ADHDChecklist.API.Features.Pets;

public class UserPetDto
{
    public Guid Id { get; set; }
    public string CustomName { get; set; } = null!;
    public int TotalXp { get; set; }
    public int CurrentLevel { get; set; }
    public int CurrentHealth { get; set; }
    public int State { get; set; } // 0: Healthy, 1: Sick, 2: Hibernate
    public string AssetUrl { get; set; } = null!; // URL file .glb cho level hiện tại
    public string EvolutionName { get; set; } = null!;
    public int XpToNextLevel { get; set; } // XP cần để lên cấp tiếp theo
}

public class PetStatusDto
{
    public UserPetDto? Pet { get; set; }
    public long Coins { get; set; }
}

using System;

namespace ADHDChecklist.Client.Features.Pets.Models;

public record UserPetDto(
    Guid Id,
    string CustomName,
    int TotalXp,
    int CurrentLevel,
    int CurrentHealth,
    int State, // 0: Healthy, 1: Sick, 2: Hibernate
    string AssetUrl,
    string EvolutionName,
    int XpToNextLevel
);

public record PetStatusDto(
    UserPetDto? Pet,
    long Coins
);

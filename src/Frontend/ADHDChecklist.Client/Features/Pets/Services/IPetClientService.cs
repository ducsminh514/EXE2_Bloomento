using ADHDChecklist.Client.Features.Pets.Models;
using System;
using System.Threading.Tasks;

namespace ADHDChecklist.Client.Features.Pets.Services;

public interface IPetClientService
{
    event Action? OnPetStatusUpdated;
    Task<PetStatusDto?> GetMyPetAsync();
    Task UpdatePetNameAsync(string newName);
    Task RecoverPetAsync();
    Task RefreshStatusAsync();
}

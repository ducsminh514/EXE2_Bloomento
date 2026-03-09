using System.Net.Http.Json;
using ADHDChecklist.Client.Features.Pets.Models;

namespace ADHDChecklist.Client.Features.Pets.Services;

public class PetClientService : IPetClientService
{
    private readonly HttpClient _http;
    public event Action? OnPetStatusUpdated;

    public PetClientService(HttpClient http)
    {
        _http = http;
    }

    public async Task<PetStatusDto?> GetMyPetAsync()
    {
        var status = await _http.GetFromJsonAsync<PetStatusDto>("api/pets/my-pet");
        return status;
    }

    public async Task UpdatePetNameAsync(string newName)
    {
        await _http.PatchAsJsonAsync("api/pets/my-pet/name", new { NewName = newName });
        await RefreshStatusAsync();
    }

    public async Task RecoverPetAsync()
    {
        await _http.PostAsync("api/pets/my-pet/recover", null);
        await RefreshStatusAsync();
    }

    public async Task RefreshStatusAsync()
    {
        OnPetStatusUpdated?.Invoke();
        await Task.CompletedTask;
    }
}

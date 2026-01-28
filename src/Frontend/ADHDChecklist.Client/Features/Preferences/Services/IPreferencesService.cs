using ADHDChecklist.Client.Shared.Models;

namespace ADHDChecklist.Client.Features.Preferences.Services;

public interface IPreferencesService
{
    Task<PreferencesResponse?> GetPreferencesAsync();
    Task<bool> UpdatePreferencesAsync(string? themeColor, string? themeMode);
    
    // State management
    event Action? OnChange;
    string CurrentThemeColor { get; }
    string CurrentThemeMode { get; }
}

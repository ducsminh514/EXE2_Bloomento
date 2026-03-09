using System.Net.Http.Json;
using ADHDChecklist.Client.Infrastructure.Services;
using ADHDChecklist.Client.Shared.Models;
using Microsoft.JSInterop;

namespace ADHDChecklist.Client.Features.Preferences.Services;

public class PreferencesService : IPreferencesService
{
    private readonly IApiClient _apiClient;
    private readonly IJSRuntime _js;

    public PreferencesService(IApiClient apiClient, IJSRuntime js)
    {
        _apiClient = apiClient;
        _js = js;
    }

    public string CurrentThemeColor { get; private set; } = "indigo";
    public string CurrentThemeMode { get; private set; } = "light";

    public event Action? OnChange;

    public async Task<PreferencesResponse?> GetPreferencesAsync()
    {
        try
        {
            var response = await _apiClient.GetAsync<PreferencesResponse>("/api/preferences");
            if (response != null)
            {
                CurrentThemeColor = response.ThemeColor ?? "indigo";
                CurrentThemeMode = response.ThemeMode ?? "light";
                
                // Cache to LocalStorage for fast boot
                await _js.InvokeVoidAsync("localStorage.setItem", "theme-color", CurrentThemeColor);
                await _js.InvokeVoidAsync("localStorage.setItem", "theme-mode", CurrentThemeMode);
                
                NotifyStateChanged();
                await ApplyThemeAsync();
            }
            return response;
        }
        catch (Exception)
        {
            // Log but fallback to defaults or local storage if we can read it synchronously (not easy in WASM service without JSRuntime ready)
            return null;
        }
    }

    public async Task<bool> UpdatePreferencesAsync(string? themeColor, string? themeMode)
    {
        try
        {
            var request = new UpdatePreferencesRequest(themeColor, themeMode);
            var response = await _apiClient.PutAsync<object>("/api/preferences", request);
            
            if (themeColor != null) 
            {
                CurrentThemeColor = themeColor;
                await _js.InvokeVoidAsync("localStorage.setItem", "theme-color", themeColor);
            }
            if (themeMode != null) 
            {
                CurrentThemeMode = themeMode;
                await _js.InvokeVoidAsync("localStorage.setItem", "theme-mode", themeMode);
            }
            
            NotifyStateChanged();
            await ApplyThemeAsync();
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task ApplyThemeAsync()
    {
        // Apply variables via JS or body class
        // For Theme Color: remove all theme-* classes, add new one
        await _js.InvokeVoidAsync("document.body.classList.remove", 
            "theme-indigo", "theme-rose", "theme-amber", "theme-emerald", "theme-sky", "theme-violet");
        
        await _js.InvokeVoidAsync("document.body.classList.add", $"theme-{CurrentThemeColor}");
        
        // For Dark Mode
        if (CurrentThemeMode == "dark")
        {
            await _js.InvokeVoidAsync("document.documentElement.classList.add", "dark");
        }
        else
        {
            await _js.InvokeVoidAsync("document.documentElement.classList.remove", "dark");
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}

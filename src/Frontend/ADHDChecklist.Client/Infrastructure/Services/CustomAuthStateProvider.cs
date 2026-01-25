using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace ADHDChecklist.Client.Infrastructure.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<CustomAuthStateProvider> _logger;

    public CustomAuthStateProvider(
        ILocalStorageService localStorage,
        ILogger<CustomAuthStateProvider> logger)
    {
        _localStorage = localStorage;
        _logger = logger;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>("accessToken");

            if (string.IsNullOrEmpty(token))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            // Parse JWT token
            var claims = ParseClaimsFromJwt(token);
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting authentication state");
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public void NotifyUserAuthentication()
    {
        var authState = GetAuthenticationStateAsync();
        NotifyAuthenticationStateChanged(authState);
    }

    public void NotifyUserLogout()
    {
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        var authState = Task.FromResult(new AuthenticationState(anonymousUser));
        NotifyAuthenticationStateChanged(authState);
    }

    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        var payload = jwt.Split('.')[1];

        // Base64 decode
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        if (keyValuePairs != null)
        {
            // Extract standard claims
            if (keyValuePairs.TryGetValue(ClaimTypes.NameIdentifier, out var userId))
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.ToString()!));

            if (keyValuePairs.TryGetValue(ClaimTypes.Email, out var email))
                claims.Add(new Claim(ClaimTypes.Email, email.ToString()!));

            if (keyValuePairs.TryGetValue(ClaimTypes.Name, out var name))
                claims.Add(new Claim(ClaimTypes.Name, name.ToString()!));

            // Custom claims
            if (keyValuePairs.TryGetValue("SubscriptionTier", out var tier))
                claims.Add(new Claim("SubscriptionTier", tier.ToString()!));

            if (keyValuePairs.TryGetValue("IsPremium", out var isPremium))
                claims.Add(new Claim("IsPremium", isPremium.ToString()!));

            // Add expiration
            if (keyValuePairs.TryGetValue("exp", out var exp))
                claims.Add(new Claim("exp", exp.ToString()!));
        }

        return claims;
    }

    private byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}
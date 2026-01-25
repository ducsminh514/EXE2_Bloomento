using ADHDChecklist.Client;
using ADHDChecklist.Client.Features.Auth.Services;
using ADHDChecklist.Client.Infrastructure.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ============================================
// HTTP CLIENT
// ============================================
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:7001")
});

// ============================================
// LOCAL STORAGE
// ============================================
builder.Services.AddBlazoredLocalStorage();

// ============================================
// AUTH SERVICES
// ============================================
builder.Services.AddScoped<IApiClient, ApiClient>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
    provider.GetRequiredService<CustomAuthStateProvider>());

builder.Services.AddAuthorizationCore();

// ============================================
// LOGGING
// ============================================
builder.Logging.SetMinimumLevel(LogLevel.Information);
await builder.Build().RunAsync();

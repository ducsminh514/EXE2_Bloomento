using ADHDChecklist.Client;
using ADHDChecklist.Client.Features.Auth.Services;
using ADHDChecklist.Client.Features.Dashboard.Services;
using ADHDChecklist.Client.Features.Preferences.Services;
using ADHDChecklist.Client.Features.Habits.Services;
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

builder.Services.AddScoped<DragDropState>();

// ============================================
// AUTH SERVICES
// ============================================
builder.Services.AddScoped<IApiClient, ApiClient>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IHabitService, HabitService>();
builder.Services.AddScoped<IPreferencesService, PreferencesService>();
builder.Services.AddAuthorizationCore();

// ============================================
// LOGGING
// ============================================
builder.Logging.SetMinimumLevel(LogLevel.Information);
await builder.Build().RunAsync();

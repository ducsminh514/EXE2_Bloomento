using ADHDChecklist.Client;
using ADHDChecklist.Client.Features.Auth.Services;
using ADHDChecklist.Client.Features.Dashboard.Services;
using ADHDChecklist.Client.Features.Preferences.Services;
using ADHDChecklist.Client.Features.Habits.Services;
using ADHDChecklist.Client.Features.Knowledge.Services;
using ADHDChecklist.Client.Infrastructure.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ADHDChecklist.Client.Features.Family.Services;
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
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// ============================================
// FEATURE SERVICES
// ============================================
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IRewardService, RewardService>(); // Added IRewardService registration
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
//builder.Services.AddScoped<IPremiumAnalyticsService, PremiumAnalyticsService>();
builder.Services.AddScoped<IBrainDumpService, BrainDumpService>();
builder.Services.AddScoped<IHabitService, HabitService>();
builder.Services.AddScoped<IKnowledgeService, KnowledgeService>();
builder.Services.AddScoped<IPreferencesService, PreferencesService>();
builder.Services.AddScoped<ADHDChecklist.Client.Features.Admin.Services.IAdminService, ADHDChecklist.Client.Features.Admin.Services.AdminService>();
builder.Services.AddScoped<ADHDChecklist.Client.Features.Family.Services.IFamilyService, ADHDChecklist.Client.Features.Family.Services.FamilyService>();
builder.Services.AddScoped<ADHDChecklist.Client.Features.Notifications.Services.INotificationService, ADHDChecklist.Client.Features.Notifications.Services.NotificationService>();

builder.Services.AddAuthorizationCore();

// ============================================
// LOGGING
// ============================================
builder.Logging.SetMinimumLevel(LogLevel.Information);
await builder.Build().RunAsync();

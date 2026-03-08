using System.Text;
using ADHDChecklist.API.Features.Tasks.AutoAdjust;
using Hangfire;
using Hangfire.PostgreSql; // Npgsql Hangfire storage
using Microsoft.EntityFrameworkCore;
using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using ADHDChecklist.API.Entities.Common;
using ADHDChecklist.API.Features.Analytics.FreeTier;
using ADHDChecklist.API.Features.Analytics.Premium;
using ADHDChecklist.API.Features.Auth.GoogleLogin;
using ADHDChecklist.API.Features.Auth.Login;
using ADHDChecklist.API.Features.Auth.RefreshToken;
using ADHDChecklist.API.Features.Auth.Register;
using ADHDChecklist.API.Features.Auth.ResendVerification;
using ADHDChecklist.API.Features.Auth.VerifyEmail;
using System.Text.Json.Serialization;
using ADHDChecklist.API.Features.Knowledge.Categories;
using ADHDChecklist.API.Features.Knowledge.Articles;
using ADHDChecklist.API.Features.Knowledge.Comments;
using ADHDChecklist.API.Features.Common.Upload;
using ADHDChecklist.API.Features.Admin.Dashboard;
using ADHDChecklist.API.Features.Admin.Users;
using ADHDChecklist.API.Features.Knowledge.Bookmarks;
using ADHDChecklist.API.Features.Categories;
using ADHDChecklist.API.Features.Tasks.CreateTask;
using ADHDChecklist.API.Features.Tasks.DeleteTask;
using ADHDChecklist.API.Features.Tasks.GetTaskById;
using ADHDChecklist.API.Features.Tasks.GetTasksByDate;
using ADHDChecklist.API.Features.Tasks.MoveTask;
using ADHDChecklist.API.Features.Tasks.ToggleTask;
using ADHDChecklist.API.Features.Tasks.UpdateTask;
using ADHDChecklist.API.Features.Tasks.RespondAssignment;
using ADHDChecklist.API.Features.Preferences.GetPreferences;
using ADHDChecklist.API.Features.Preferences.UpdatePreferences;
using ADHDChecklist.API.Features.BrainDump.CreateBrainDumpItem;
using ADHDChecklist.API.Features.BrainDump.GetBrainDumpItems;
using ADHDChecklist.API.Features.BrainDump.DeleteBrainDumpItem;
using ADHDChecklist.API.Features.Tasks.ApproveCompletion;
using ADHDChecklist.API.Features.Tasks.RequestRework;
using ADHDChecklist.API.Features.Tasks.GetOverdueCount;
using ADHDChecklist.API.Features.Users.UpdateProfile;
using ADHDChecklist.API.Features.Users.Upgrade;
using ADHDChecklist.API.Features.Habits.GetHabits;
using ADHDChecklist.API.Features.Habits.CreateHabit;
using ADHDChecklist.API.Features.Habits.ToggleHabit;
using ADHDChecklist.API.Features.Family.CreateFamily;
using ADHDChecklist.API.Features.Family.GetFamily;
using ADHDChecklist.API.Features.Family.InviteMember;
using ADHDChecklist.API.Features.Family.JoinFamily;
using ADHDChecklist.API.Features.Family.RemoveMember;
using ADHDChecklist.API.Features.Family.UpdateMemberRole;
using ADHDChecklist.API.Features.Notifications.GetNotifications;
using ADHDChecklist.API.Features.Notifications.MarkAsRead;
using ADHDChecklist.API.Features.Family.Gamification.GetPoints;
using ADHDChecklist.API.Features.Family.Gamification.ManageRewards;
using ADHDChecklist.API.Features.Payments.CreatePayment;
using ADHDChecklist.API.Features.Payments.Webhook;
using ADHDChecklist.API.Services;
using ADHDChecklist.API.Services.BackgroundJobs;
using ADHDChecklist.API.Shared.Behaviors;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ADHDChecklist.API.Features.AI.BreakdownTask;
using ADHDChecklist.API.Features.Focus;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.RateLimiting;
using PayOS;

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Configure Upload Limits (50MB)
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 52428800; // 50MB
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52428800; // 50MB
});
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

string connectionString;

if (!string.IsNullOrEmpty(databaseUrl))
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');

    connectionString =
        $"Host={uri.Host};" +
        $"Port={uri.Port};" +
        $"Database={uri.AbsolutePath.Trim('/')};" +
        $"Username={userInfo[0]};" +
        $"Password={userInfo[1]};" +
        $"SSL Mode=Prefer;Trust Server Certificate=true";
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}
// ============================================
// 1. DATABASE
// ============================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// ============================================
// 2. IDENTITY
// ============================================
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false; // We handle this manually
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ============================================
// 3. JWT AUTHENTICATION
// ============================================
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
});

// ============================================
// 4. CORS
// ============================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.SetIsOriginAllowed(origin => 
                origin.StartsWith("http://localhost:") || 
                origin.StartsWith("https://localhost:") || 
                origin.EndsWith(".vercel.app"))
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});



// ============================================
// 5. MEDIATر & VALIDATION
// ============================================
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// ============================================
// ============================================
// 6. SERVICES

builder.Services.AddExceptionHandler<ADHDChecklist.API.Shared.Middleware.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ============================================
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ADHDChecklist.API.Services.BackgroundJobs.CleanupService>();
builder.Services.AddScoped<ADHDChecklist.API.Services.BackgroundJobs.ReminderJob>();
builder.Services.AddHttpClient<IGeminiService, GeminiService>();
builder.Services.AddScoped<KnowledgeSeeder>();
builder.Services.AddScoped<IdentitySeeder>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// PayOS Configuration
var payOSSettings = builder.Configuration.GetSection("PayOS");
PayOSClient payOS = new PayOSClient(
    payOSSettings["ClientId"] ?? "",
    payOSSettings["ApiKey"] ?? "",
    payOSSettings["ChecksumKey"] ?? ""
);
builder.Services.AddSingleton(payOS);



builder.Services.AddMemoryCache();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("GeminiPolicy", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
});

// ============================================
// 6.1 HANGFIRE
// ============================================
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options =>
        options.UseNpgsqlConnection(connectionString)));

builder.Services.AddHangfireServer();

// ============================================
// 7. SWAGGER
// ============================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ADHD Checklist API",
        Version = "v1",
        Description = "API for ADHD task management application"
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ============================================
// 8. MIDDLEWARE PIPELINE
// ============================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ADHD Checklist API v1");
    });
}

app.UseExceptionHandler();
app.UseStaticFiles(); // Serves wwwroot by default

// Ensure uploads are served even if outside typical structure or to be explicit
var uploadsPath = Path.Combine(builder.Environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads");
if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});
//app.UseHttpsRedirection();
app.UseCors("AllowBlazorClient");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// ============================================
// 9. ENDPOINTS
// ============================================

// Health check
app.MapGet("/", () => "ADHD Checklist API v1.0");

app.MapGet("/health", async (AppDbContext db) =>
{
    try
    {
        await db.Database.CanConnectAsync();
        return Results.Ok(new { status = "healthy", database = "connected", timestamp = DateTime.UtcNow });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 503,
            title: "Database connection failed"
        );
    }
});

// Auth endpoints
app.MapRegister();
app.MapLogin();
app.MapVerifyEmail();
app.MapResendVerification();
app.MapGoogleLogin();
app.MapRefreshToken();

// Task endpoints
app.MapGetTaskById();
app.MapGetTasksByDate();
app.MapCreateTask();
app.MapUpdateTask();
app.MapDeleteTask();
app.MapToggleTaskCompletion();
app.MapMoveTaskToTimeSlot();
app.MapMoveTaskToInbox();
app.MapUpdateTaskOrder();
app.MapRespondAssignment();
app.MapAutoAdjustTasks();
app.MapApproveTaskCompletion();
app.MapRequestTaskRework();
app.MapGetOverdueCount();
// Category endpoints
app.MapCategoryEndpoints();

// Knowledge endpoints
app.MapGetKnowledgeCategories();
app.MapArticleEndpoints();
app.MapArticleDetailEndpoint();
app.MapGetCommentsEndpoint();
app.MapCreateCommentEndpoint();
app.MapToggleBookmarkEndpoint();
app.MapGetBookmarkedArticlesEndpoint();

// Admin Knowledge (Admin Policy required)
app.MapGetAdminArticlesEndpoint();
app.MapCreateArticleEndpoint();
app.MapUpdateArticleEndpoint();
app.MapDeleteArticleEndpoint();
// Admin Comments
app.MapGetAdminCommentsEndpoint();
app.MapToggleCommentVisibilityEndpoint();
app.MapDeleteCommentEndpoint();
// Admin Categories
app.MapCreateCategoryEndpoint();
app.MapUpdateCategoryEndpoint();
app.MapDeleteCategoryEndpoint();

// Common
app.MapUploadImageEndpoint();

// Admin Dashboard
app.MapGetAdminDashboardStatsEndpoint();
app.MapGetAdminDashboardChartsEndpoint();
app.MapGetUsersEndpoint();
app.MapGetUserDetailEndpoint();
app.MapToggleUserLockEndpoint();

// User endpoints
app.MapUpdateProfile();
app.MapUpgrade();
app.MapGetPreferences();
app.MapUpdatePreferences();

// Analytics endpoints
app.MapAnalyticsEndpoints();
app.MapPremiumAnalyticsEndpoints();

// Habit endpoints
app.MapGetHabits();
app.MapCreateHabit();
app.MapToggleHabit();
app.MapCreateBrainDumpItem();
app.MapGetBrainDumpItems();
app.MapDeleteBrainDumpItem();
app.MapCreateFamily();
app.MapGetFamily();
app.MapInviteMember();
app.MapJoinFamily();
app.MapRemoveMember();
app.MapUpdateMemberRole();

app.MapGetNotifications();
app.MapMarkAsRead();
app.MapGetPoints();
app.MapManageRewards();

// Payment endpoints
app.MapCreatePayment();
app.MapPayOSWebhook();

app.MapBreakdownTask();
app.MapFocusSessionEndpoints();




// ============================================
// 10. BACKGROUND JOBS
// ============================================
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new AllowAllDashboardAuthorizationFilter() }
});

// Schedule Cleanup Job (Daily at 2 AM)
RecurringJob.AddOrUpdate<ADHDChecklist.API.Services.BackgroundJobs.CleanupService>(
    "free-tier-cleanup",
    service => service.DeleteOldFreeTierTasks(),
    Cron.Daily(2));

// Schedule Reminder Job (Every 15 minutes)
RecurringJob.AddOrUpdate<ADHDChecklist.API.Services.BackgroundJobs.ReminderJob>(
    "task-reminders",
    service => service.CheckAndSendReminders(),
    "*/15 * * * *");


// ============================================
// 11. DATA SEEDING
// ============================================
using (var scope = app.Services.CreateScope())
{
    try 
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate(); // Optional: Auto-migrate
        
        var knowledgeSeeder = scope.ServiceProvider.GetRequiredService<KnowledgeSeeder>();
        await knowledgeSeeder.SeedAsync();

        var identitySeeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        await identitySeeder.SeedAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error seeding data: {ex.Message}");
    }
}

app.Run();

public class AllowAllDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return true; // DANGER: For debugging only!
    }
}
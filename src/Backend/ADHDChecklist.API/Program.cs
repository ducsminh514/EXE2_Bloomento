using System.Text;
using ADHDChecklist.API.Features.Tasks.AutoAdjust;
using Hangfire;
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
using ADHDChecklist.API.Features.Categories;
using ADHDChecklist.API.Features.Tasks.CreateTask;
using ADHDChecklist.API.Features.Tasks.DeleteTask;
using ADHDChecklist.API.Features.Tasks.GetTaskById;
using ADHDChecklist.API.Features.Tasks.GetTasksByDate;
using ADHDChecklist.API.Features.Tasks.MoveTask;
using ADHDChecklist.API.Features.Tasks.ToggleTask;
using ADHDChecklist.API.Features.Tasks.UpdateTask;
using ADHDChecklist.API.Features.Preferences.GetPreferences;
using ADHDChecklist.API.Features.Preferences.UpdatePreferences;
using ADHDChecklist.API.Features.BrainDump.CreateBrainDumpItem;
using ADHDChecklist.API.Features.BrainDump.GetBrainDumpItems;
using ADHDChecklist.API.Features.BrainDump.DeleteBrainDumpItem;
using ADHDChecklist.API.Features.Tasks.GetOverdueCount;
using ADHDChecklist.API.Features.Users.UpdateProfile;
using ADHDChecklist.API.Features.Users.Upgrade;
using ADHDChecklist.API.Features.Habits.GetHabits;
using ADHDChecklist.API.Features.Habits.CreateHabit;
using ADHDChecklist.API.Features.Habits.ToggleHabit;
using ADHDChecklist.API.Services;
using ADHDChecklist.API.Shared.Behaviors;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using ADHDChecklist.API.Features.Users.UpdateProfile;
using ADHDChecklist.API.Features.Users.Upgrade;
using ADHDChecklist.API.Features.Habits.GetHabits;
using ADHDChecklist.API.Features.Habits.CreateHabit;
using ADHDChecklist.API.Features.Habits.ToggleHabit;
using ADHDChecklist.API.Services;
using ADHDChecklist.API.Shared.Behaviors;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// 1. DATABASE
// ============================================
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

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

builder.Services.AddAuthorization();

// ============================================
// 4. CORS
// ============================================
var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() 
    ?? new[] { "http://localhost:7002", "https://localhost:7002" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.WithOrigins(allowedOrigins)
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

// ============================================
// 6.1 HANGFIRE
// ============================================
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

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
//app.UseHttpsRedirection();
app.UseCors("AllowBlazorClient");
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
app.MapAutoAdjustTasks();
app.MapGetOverdueCount();
// Category endpoints
app.MapCategoryEndpoints();

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


// ============================================
// 10. BACKGROUND JOBS
// ============================================
app.UseHangfireDashboard();

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

app.Run();
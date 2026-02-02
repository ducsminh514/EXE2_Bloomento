namespace ADHDChecklist.Client.Shared.Models;

// ============================================
// AUTH REQUEST MODELS
// ============================================
// ============================================
// AUTH REQUEST MODELS
// ============================================
public record RegisterRequest(
    string Email,
    string Password,
    string FullName
);

public record LoginRequest(
    string Email,
    string Password,
    bool RememberMe = false
);

public record GoogleLoginRequest(
    string GoogleIdToken
);

public record RefreshTokenRequest(
    string RefreshToken
);

public record UpdateProfileRequest(
    string FullName
);

public record UpgradeRequest(
    SubscriptionTier Tier
);

public record UpdateProfileResponse(
    bool Success,
    string Message,
    string? NewFullName = null
);

public record UpgradeResponse(
    bool Success,
    string Message,
    DateTime? ExpiryDate
);


// ============================================
// AUTH RESPONSE MODELS
// ============================================
public record RegisterResponse(
    bool Success,
    string Message,
    string? UserId = null
);

public record LoginResponse(
    bool Success,
    string Message,
    string? AccessToken = null,
    string? RefreshToken = null,
    UserInfo? User = null,
    bool RequireEmailVerification = false
);

public record GoogleLoginResponse(
    bool Success,
    string Message,
    string? AccessToken = null,
    string? RefreshToken = null,
    UserInfo? User = null,
    bool IsNewUser = false
);

public record RefreshTokenResponse(
    bool Success,
    string Message,
    string? AccessToken = null,
    string? RefreshToken = null
);

public record VerifyEmailResponse(
    bool Success,
    string Message
);

public record UserInfo(
    string UserId,
    string Email,
    string FullName,
    string SubscriptionTier,
    bool IsPremium,
    bool IsEmailVerified,
    string Role
);

// ============================================
// USER STATE MODEL
// ============================================
public class CurrentUser
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string SubscriptionTier { get; set; } = "Free";
    public bool IsPremium { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsAuthenticated { get; set; }
    public string Role { get; set; } = "Member";
}

// ============================================
// API RESPONSE WRAPPER
// ============================================
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
}

// ============================================
// TASK MODELS
// ============================================
public record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    Guid? CategoryId,
    string? CategoryName,
    string? CategoryColor,
    DateOnly ScheduledDate,
    TimeOnly? TimeBlockStart,
    TimeOnly? TimeBlockEnd,
    int? Duration,
    bool IsCompleted,
    DateTime? CompletedAt,
    int Priority,
    bool IsRecurring,
    string? RecurrencePattern,
    Guid? ParentTaskId,
    List<TaskResponse> SubTasks,
    int? OrderIndex,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int? RescheduleCount = 0,
    string? DopamineType = "Low",
    Guid? FamilyId = null,
    Guid? AssignedUserId = null,
    string? AssignedUserName = null,
    string? AssignedUserAvatar = null,
    bool IsShared = false
);

public record TaskListResponse(
    List<TaskResponse> Tasks,
    int TotalCount,
    DateOnly Date
);

public record CreateTaskRequest(
    string Title,
    string? Description,
    Guid? CategoryId,
    DateOnly ScheduledDate,
    TimeOnly? TimeBlockStart,
    TimeOnly? TimeBlockEnd,
    int? Duration,
    int Priority,
    bool IsRecurring,
    string? RecurrencePattern,
    string? DopamineType = "Low",
    Guid? FamilyId = null,
    Guid? AssignedUserId = null,
    bool IsShared = false
);

public record UpdateTaskRequest(
    string Title,
    string? Description,
    Guid? CategoryId,
    DateOnly ScheduledDate,
    TimeOnly? TimeBlockStart,
    TimeOnly? TimeBlockEnd,
    int? Duration,
    int Priority,
    bool IsCompleted,
    string? DopamineType = null,
    Guid? AssignedUserId = null,
    bool IsShared = false
);

// ============================================
// CATEGORY MODELS
// ============================================
public record CategoryResponse(
    Guid Id,
    string Name,
    string ColorHex,
    string? Icon,
    int OrderIndex,
    int TaskCount
);

public record CreateCategoryRequest(
    string Name,
    string ColorHex,
    string? Icon
);

public record UpdateCategoryRequest(
    string Name,
    string ColorHex,
    string? Icon
);

// ============================================
// ANALYTICS MODELS
// ============================================
public record WeeklyAnalyticsResponse(
    List<DailyStats> DailyStats,
    int TotalCompleted,
    int TotalCreated,
    double CompletionRate
);

public record DailyStats(
    DateOnly Date,
    string DayOfWeek,
    int TasksCompleted,
    int TasksCreated
);

// --- PREMIUM ANALYTICS MODELS ---
public record PremiumAnalyticsResponse(
    List<TimeBlindnessData> TimeBlindness,
    List<EnergyData> EnergyHeatmap,
    List<ProcrastinationDebtData> ProcrastinationDebt,
    DopamineBalanceData DopamineBalance
);

public record TimeBlindnessData(
    string TaskTitle,
    int EstimatedMinutes,
    int ActualMinutes,
    double DeviationPercentage
);

public record EnergyData(
    int Hour,
    int CompletedCount
);

public record ProcrastinationDebtData(
    Guid TaskId,
    string Title,
    int RescheduleCount
);

public record DopamineBalanceData(
    int LowDopamineCount,
    int HighDopamineCount,
    double BalanceRatio
);

// ============================================
// ENUMS
// ============================================
public enum SubscriptionTier
{
    Free = 0,
    Premium = 1
}

// ============================================
// HABIT MODELS
// ============================================
public record HabitResponse(
    Guid Id,
    string Title,
    string? Description,
    string? ColorHex,
    string? Icon,
    string Frequency, // "daily", "weekly"
    int CurrentStreak,
    int LongestStreak,
    List<DateOnly> CompletedDates
);

public class CreateHabitRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ColorHex { get; set; }
    public string? Icon { get; set; }
    public string Frequency { get; set; } = "daily";
}

public record ToggleHabitRequest(
    DateOnly Date
);

// ============================================
// PREFERENCE MODELS
// ============================================
public record PreferencesResponse(
    string? ThemeColor,
    string? ThemeMode
);

public record UpdatePreferencesRequest(
    string? ThemeColor,
    string? ThemeMode
);

// ============================================
// BRAIN DUMP MODELS
// ============================================
public record BrainDumpItemResponse(
    Guid Id,
    string Content,
    DateTime CreatedAt
);

public record CreateBrainDumpItemRequest(
    string Content
);
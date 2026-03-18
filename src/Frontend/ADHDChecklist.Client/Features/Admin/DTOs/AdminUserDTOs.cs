namespace ADHDChecklist.Client.Features.Admin.DTOs;

public class UserListResponse
{
    public List<UserDto> Users { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string SubscriptionTier { get; set; } = "Free";
    public bool IsPremium { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsLockedOut { get; set; }
}

public class UserStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int UnverifiedUsers { get; set; }
    public int LockedUsers { get; set; }
    public int NewUsersToday { get; set; }
    public int NewUsersThisWeek { get; set; }
    public int PremiumUsers { get; set; }
}

public class UserDetailResponse
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string SubscriptionTier { get; set; } = "Free";
    public bool IsPremium { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsLockedOut { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public int TaskCount { get; set; }
    public int CompletedTaskCount { get; set; }
    public int ArticleReadCount { get; set; }
    public string CurrentStreak { get; set; } = "0 days";
}

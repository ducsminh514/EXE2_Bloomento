using Microsoft.AspNetCore.Identity;
using ADHDChecklist.API.Entities;

namespace ADHDChecklist.API.Entities.Common;

public class ApplicationUser : IdentityUser<Guid>
{
    // Custom properties
    public string? FullName { get; set; }

    // Subscription
    public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Free;
    public DateTime? SubscriptionExpiry { get; set; }
    public SubscriptionTier? PreviousSubscriptionTier { get; set; }

    // Email verification
    public bool IsEmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpiry { get; set; }

    // Tracking
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public string TimeZone { get; set; } = "UTC";
    public bool IsActive { get; set; } = true;

    // Google OAuth
    public string? GoogleId { get; set; }
    public string? GoogleProfilePicture { get; set; }
    
    // AI Usage
    public int LifetimeAiUsageCount { get; set; } = 0;
    
    // Gamification
    public int TotalXp { get; set; } = 0;

    // Refresh Token
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }

    // Navigation properties
    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
    public virtual ICollection<Habit> Habits { get; set; } = new List<Habit>();
    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
    public virtual ICollection<FocusSession> FocusSessions { get; set; } = new List<FocusSession>();
    public virtual ICollection<BrainDumpItem> BrainDumpItems { get; set; } = new List<BrainDumpItem>();
    public virtual ICollection<UserPreference?> Preferences { get; set; } = new List<UserPreference?>();
    
    // Family
    public virtual ICollection<FamilyMember> FamilyMembers { get; set; } = new List<FamilyMember>();

    // Helper methods
    public bool IsPremium()
    {
        return (SubscriptionTier == SubscriptionTier.Premium || SubscriptionTier == SubscriptionTier.Family)
            && (SubscriptionExpiry == null || SubscriptionExpiry > DateTime.UtcNow);
    }

    public bool IsEmailVerificationValid()
    {
        return !string.IsNullOrEmpty(EmailVerificationToken)
            && EmailVerificationTokenExpiry.HasValue
            && EmailVerificationTokenExpiry.Value > DateTime.UtcNow;
    }

    public bool IsRefreshTokenValid()
    {
        return !string.IsNullOrEmpty(RefreshToken)
            && RefreshTokenExpiry.HasValue
            && RefreshTokenExpiry.Value > DateTime.UtcNow;
    }
}

public enum SubscriptionTier
{
    Free = 0,
    Premium = 1,
    Family = 2
}
using System.Reflection;
using ADHDChecklist.API.Entities;
using ADHDChecklist.API.Entities.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<ADHDChecklist.API.Entities.Task> Tasks { get; set; } = null!;
    public DbSet<Habit> Habits { get; set; } = null!;
    public DbSet<HabitCompletion> HabitCompletions { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Reminder> Reminders { get; set; } = null!;
    public DbSet<FocusSession> FocusSessions { get; set; } = null!;
    public DbSet<DistractionLog> DistractionLogs { get; set; } = null!;
    public DbSet<BrainDumpItem> BrainDumpItems { get; set; } = null!;
    public DbSet<UserPreference> UserPreferences { get; set; } = null!;
    public DbSet<AnalyticsSnapshot> AnalyticsSnapshots { get; set; } = null!;
    
    // Knowledge Sharing Module
    public DbSet<KnowledgeCategory> KnowledgeCategories { get; set; } = null!;
    public DbSet<Article> Articles { get; set; } = null!;
    public DbSet<ArticleComment> ArticleComments { get; set; } = null!;
    public DbSet<ArticleBookmark> ArticleBookmarks { get; set; } = null!;
    public DbSet<ReadingProgress> ReadingProgresses { get; set; } = null!;

    // Family Module
    public DbSet<Family> Families { get; set; } = null!;
    public DbSet<FamilyMember> FamilyMembers { get; set; } = null!;
    public DbSet<FamilyInvitation> FamilyInvitations { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<FamilyReward> FamilyRewards { get; set; } = null!;
    public DbSet<FamilyPointHistory> FamilyPointHistory { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Rename Identity tables to avoid conflicts
        modelBuilder.Entity<ApplicationUser>().ToTable("Users");
        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("Roles");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");

        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // FAMILY CONFIGURATION
        modelBuilder.Entity<Family>()
            .HasOne(f => f.Owner)
            .WithMany()
            .HasForeignKey(f => f.OwnerId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent deleting User if they own a Family

        // 1. Family -> Members: RESTRICT (Must clear members before deleting family)
        // This avoids SQL Server "Multiple Cascade Paths" error.
        modelBuilder.Entity<FamilyMember>()
            .HasOne(fm => fm.Family)
            .WithMany(f => f.Members)
            .HasForeignKey(fm => fm.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        // 2. User -> Members: RESTRICT (User must leave family before deleting account)
        // This breaks the cycle: Family -> Member (Cascade) AND User -> Family (Owner) -> Member (Cascade via Family)
        modelBuilder.Entity<FamilyMember>()
            .HasOne(fm => fm.User)
            .WithMany(u => u.FamilyMembers)
            .HasForeignKey(fm => fm.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FamilyInvitation>()
            .HasOne(fi => fi.Family)
            .WithMany(f => f.Invitations)
            .HasForeignKey(fi => fi.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        // TASKS & HABITS
        // Avoid cascading from both Family and User
        // Strategy: SetNull for Family-linked items. Data remains but unlinked.

        modelBuilder.Entity<ADHDChecklist.API.Entities.Task>()
            .HasOne(t => t.Family)
            .WithMany()
            .HasForeignKey(t => t.FamilyId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ADHDChecklist.API.Entities.Task>()
            .HasOne(t => t.AssignedUser)
            .WithMany()
            .HasForeignKey(t => t.AssignedUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Habit>()
            .HasOne(h => h.Family)
            .WithMany()
            .HasForeignKey(h => h.FamilyId)
            .OnDelete(DeleteBehavior.SetNull);

        // Knowledge Module - Avoid Cascade Cycles
        
        // Article -> User (Author) : Restrict (Don't delete articles if user is deleted, or handle manually)
        modelBuilder.Entity<Article>()
            .HasOne(a => a.Author)
            .WithMany()
            .HasForeignKey(a => a.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Comment -> User : Restrict
        modelBuilder.Entity<ArticleComment>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Bookmark -> User : Restrict
        modelBuilder.Entity<ArticleBookmark>()
            .HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Progress -> User : Restrict
        modelBuilder.Entity<ReadingProgress>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Notifications
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade); // If user deleted, notifications gone too? Or Restrict? 
            // Usually Cascade is fine for notifications as they are personal.

        // GAMIFICATION
        modelBuilder.Entity<FamilyReward>()
            .HasOne(r => r.Family)
            .WithMany()
            .HasForeignKey(r => r.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FamilyPointHistory>()
            .HasOne(ph => ph.Family)
            .WithMany()
            .HasForeignKey(ph => ph.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FamilyPointHistory>()
            .HasOne(ph => ph.User)
            .WithMany()
            .HasForeignKey(ph => ph.UserId)
            .OnDelete(DeleteBehavior.Restrict); // Keep history even if user is removed from family? Or Cascade? RESTRICT is safer.

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    // Auto-update timestamps
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditable &&
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (IAuditable)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }

            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}


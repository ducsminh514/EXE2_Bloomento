using System;
using System.Collections.Generic;
using ADHDChecklist.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AnalyticsSnapshot> AnalyticsSnapshots { get; set; }

    public virtual DbSet<BrainDumpItem> BrainDumpItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<DistractionLog> DistractionLogs { get; set; }

    public virtual DbSet<FocusSession> FocusSessions { get; set; }

    public virtual DbSet<Habit> Habits { get; set; }

    public virtual DbSet<HabitCompletion> HabitCompletions { get; set; }

    public virtual DbSet<Reminder> Reminders { get; set; }

    public virtual DbSet<ADHDChecklist.API.Entities.Task> Tasks { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserPreference> UserPreferences { get; set; }

   
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AnalyticsSnapshot>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Analytic__3214EC076C7B8F6F");

            entity.HasIndex(e => new { e.UserId, e.SnapshotDate }, "UQ__Analytic__2C7B286DBE467F44").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.HabitsCompleted).HasDefaultValue(0);
            entity.Property(e => e.TasksCompleted).HasDefaultValue(0);
            entity.Property(e => e.TasksCreated).HasDefaultValue(0);
            entity.Property(e => e.TotalFocusMinutes).HasDefaultValue(0);

            entity.HasOne(d => d.User).WithMany(p => p.AnalyticsSnapshots)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Analytics__UserI__17F790F9");
        });

        modelBuilder.Entity<BrainDumpItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__BrainDum__3214EC0729EF4467");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Content).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsProcessed).HasDefaultValue(false);

            entity.HasOne(d => d.ConvertedToTask).WithMany(p => p.BrainDumpItems)
                .HasForeignKey(d => d.ConvertedToTaskId)
                .HasConstraintName("FK__BrainDump__Conve__05D8E0BE");

            entity.HasOne(d => d.User).WithMany(p => p.BrainDumpItems)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BrainDump__UserI__03F0984C");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Categori__3214EC07B478961B");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ColorHex)
                .HasMaxLength(7)
                .HasDefaultValue("#6B7280");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.OrderIndex).HasDefaultValue(0);

            entity.HasOne(d => d.User).WithMany(p => p.Categories)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Categorie__UserI__5070F446");
        });

        modelBuilder.Entity<DistractionLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Distract__3214EC075A8CFB3D");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.DistractionType).HasMaxLength(100);
            entity.Property(e => e.LoggedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Notes).HasMaxLength(200);

            entity.HasOne(d => d.FocusSession).WithMany(p => p.DistractionLogs)
                .HasForeignKey(d => d.FocusSessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Distracti__Focus__7F2BE32F");
        });

        modelBuilder.Entity<FocusSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__FocusSes__3214EC071BE3D963");

            entity.HasIndex(e => new { e.UserId, e.StartedAt }, "IX_FocusSessions_UserId_StartedAt");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.DistractionCount).HasDefaultValue(0);
            entity.Property(e => e.FocusLevel).HasDefaultValue(2);
            entity.Property(e => e.WasCompleted).HasDefaultValue(false);
            entity.Property(e => e.WhiteNoiseUsed).HasMaxLength(50);

            entity.HasOne(d => d.Task).WithMany(p => p.FocusSessions)
                .HasForeignKey(d => d.TaskId)
                .HasConstraintName("FK__FocusSess__TaskI__778AC167");

            entity.HasOne(d => d.User).WithMany(p => p.FocusSessions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__FocusSess__UserI__76969D2E");
        });

        modelBuilder.Entity<Habit>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Habits__3214EC07FEFF5E22");

            entity.HasIndex(e => new { e.UserId, e.IsActive }, "IX_Habits_UserId_IsActive");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ColorHex)
                .HasMaxLength(7)
                .HasDefaultValue("#3B82F6");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.CurrentStreak).HasDefaultValue(0);
            entity.Property(e => e.DaysOfWeek).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Frequency).HasMaxLength(20);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LongestStreak).HasDefaultValue(0);
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.User).WithMany(p => p.Habits)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Habits__UserId__619B8048");
        });

        modelBuilder.Entity<HabitCompletion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__HabitCom__3214EC075796764F");

            entity.HasIndex(e => new { e.HabitId, e.CompletionDate }, "UQ__HabitCom__83CDEDD6833D5D17").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CompletedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(d => d.Habit).WithMany(p => p.HabitCompletions)
                .HasForeignKey(d => d.HabitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__HabitComp__Habit__6B24EA82");
        });

        modelBuilder.Entity<Reminder>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Reminder__3214EC0771520820");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsSent).HasDefaultValue(false);
            entity.Property(e => e.ReminderType)
                .HasMaxLength(20)
                .HasDefaultValue("notification");

            entity.HasOne(d => d.Task).WithMany(p => p.Reminders)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reminders__TaskI__6FE99F9F");
        });

        modelBuilder.Entity<ADHDChecklist.API.Entities.Task>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tasks__3214EC07645A646C");

            entity.HasIndex(e => e.IsCompleted, "IX_Tasks_IsCompleted");

            entity.HasIndex(e => new { e.UserId, e.ScheduledDate }, "IX_Tasks_UserId_ScheduledDate");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.IsCompleted).HasDefaultValue(false);
            entity.Property(e => e.IsRecurring).HasDefaultValue(false);
            entity.Property(e => e.OrderIndex).HasDefaultValue(0);
            entity.Property(e => e.Priority).HasDefaultValue(1);
            entity.Property(e => e.RecurrencePattern).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Category).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__Tasks__CategoryI__5812160E");

            entity.HasOne(d => d.User).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tasks__UserId__571DF1D5");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC07D6D68C1C");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534EAC3F896").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.TimeZone)
                .HasMaxLength(50)
                .HasDefaultValue("UTC");
        });

        modelBuilder.Entity<UserPreference>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__UserPref__1788CC4C9590250F");

            entity.Property(e => e.UserId).ValueGeneratedNever();
            entity.Property(e => e.AllowFlexibleBlocks).HasDefaultValue(false);
            entity.Property(e => e.CustomAccentColor).HasMaxLength(7);
            entity.Property(e => e.CustomPrimaryColor).HasMaxLength(7);
            entity.Property(e => e.DefaultFocusLevel).HasDefaultValue(2);
            entity.Property(e => e.DefaultPomodoroBreak).HasDefaultValue(5);
            entity.Property(e => e.DefaultPomodoroWork).HasDefaultValue(25);
            entity.Property(e => e.DefaultTimeBlockDuration).HasDefaultValue(30);
            entity.Property(e => e.DefaultWhiteNoise)
                .HasMaxLength(50)
                .HasDefaultValue("none");
            entity.Property(e => e.EnableReminders).HasDefaultValue(true);
            entity.Property(e => e.ReminderLeadTime).HasDefaultValue(10);
            entity.Property(e => e.ThemeId).HasDefaultValue(1);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.User).WithOne(p => p.UserPreference)
                .HasForeignKey<UserPreference>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserPrefe__UserI__09A971A2");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

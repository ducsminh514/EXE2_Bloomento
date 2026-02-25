using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ADHDChecklist.API.Entities;

namespace ADHDChecklist.API.Data.Configurations;

public class HabitConfiguration : IEntityTypeConfiguration<Habit>
{
    public void Configure(EntityTypeBuilder<Habit> builder)
    {
        builder.ToTable("Habits");

        // Primary Key
        builder.HasKey(h => h.Id);

        // Properties
        builder.Property(h => h.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(h => h.Description)
            .HasMaxLength(500);

        builder.Property(h => h.ColorHex)
            .IsRequired()
            .HasMaxLength(7)
            .HasDefaultValue("#3B82F6");

        builder.Property(h => h.Frequency)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(h => h.DaysOfWeek)
            .HasMaxLength(50);

        builder.Property(h => h.CurrentStreak)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(h => h.LongestStreak)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(h => h.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(h => h.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("now() at time zone 'utc'");

        // Relationships
        builder.HasOne(h => h.User)
            .WithMany(u => u.Habits)
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(h => new { h.UserId, h.IsActive })
            .HasDatabaseName("IX_Habits_UserId_IsActive");

        builder.HasIndex(h => h.UserId)
            .HasDatabaseName("IX_Habits_UserId");
    }
}

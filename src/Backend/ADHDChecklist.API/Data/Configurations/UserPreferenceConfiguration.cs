using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ADHDChecklist.API.Entities;

namespace ADHDChecklist.API.Data.Configurations;

public class UserPreferencesConfiguration : IEntityTypeConfiguration<UserPreference>
{
    public void Configure(EntityTypeBuilder<UserPreference> builder)
    {
        builder.ToTable("UserPreferences");

        // Primary Key - Same as UserId (One-to-One)
        builder.HasKey(up => up.UserId);

        // Properties - UI Settings
        builder.Property(up => up.ThemeId)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(up => up.CustomPrimaryColor)
            .HasMaxLength(7);

        builder.Property(up => up.CustomAccentColor)
            .HasMaxLength(7);

        // Time Block Settings
        builder.Property(up => up.DefaultTimeBlockDuration)
            .IsRequired()
            .HasDefaultValue(30);

        builder.Property(up => up.AllowFlexibleBlocks)
            .IsRequired()
            .HasDefaultValue(false);

        // Focus Mode Settings
        builder.Property(up => up.DefaultFocusLevel)
            .IsRequired()
            .HasDefaultValue(2);

        builder.Property(up => up.DefaultWhiteNoise)
            .HasMaxLength(50)
            .HasDefaultValue("none");

        builder.Property(up => up.DefaultPomodoroWork)
            .IsRequired()
            .HasDefaultValue(25);

        builder.Property(up => up.DefaultPomodoroBreak)
            .IsRequired()
            .HasDefaultValue(5);

        // Notification Settings
        builder.Property(up => up.EnableReminders)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(up => up.ReminderLeadTime)
            .IsRequired()
            .HasDefaultValue(10);

        builder.Property(up => up.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(up => up.User)
            .WithMany(u => u.Preferences)
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
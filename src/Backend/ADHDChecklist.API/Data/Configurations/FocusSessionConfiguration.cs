using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ADHDChecklist.API.Entities;

namespace ADHDChecklist.API.Data.Configurations;

public class FocusSessionConfiguration : IEntityTypeConfiguration<FocusSession>
{
    public void Configure(EntityTypeBuilder<FocusSession> builder)
    {
        builder.ToTable("FocusSessions");

        // Primary Key
        builder.HasKey(fs => fs.Id);

        // Properties
        builder.Property(fs => fs.TaskId)
            .IsRequired(false);

        builder.Property(fs => fs.StartedAt)
            .IsRequired();

        builder.Property(fs => fs.EndedAt)
            .IsRequired(false);

        builder.Property(fs => fs.PlannedDuration)
            .IsRequired();

        builder.Property(fs => fs.ActualDuration)
            .IsRequired(false);

        builder.Property(fs => fs.FocusLevel)
            .IsRequired()
            .HasDefaultValue(2);

        builder.Property(fs => fs.WhiteNoiseUsed)
            .HasMaxLength(50);

        builder.Property(fs => fs.DistractionCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(fs => fs.WasCompleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(fs => fs.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("now() at time zone 'utc'");

        // Relationships
        builder.HasOne(fs => fs.User)
            .WithMany(u => u.FocusSessions)
            .HasForeignKey(fs => fs.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ? FIX: Explicitly configure Task relationship with reverse nav to avoid shadow FK TaskId1
        builder.HasOne(fs => fs.Task)
            .WithMany(t => t.FocusSessions)
            .HasForeignKey(fs => fs.TaskId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);


        // Indexes for analytics
        builder.HasIndex(fs => new { fs.UserId, fs.StartedAt })
            .HasDatabaseName("IX_FocusSessions_UserId_StartedAt");

        builder.HasIndex(fs => fs.UserId)
            .HasDatabaseName("IX_FocusSessions_UserId");

        builder.HasIndex(fs => new { fs.UserId, fs.WasCompleted })
            .HasDatabaseName("IX_FocusSessions_UserId_WasCompleted");
    }
}

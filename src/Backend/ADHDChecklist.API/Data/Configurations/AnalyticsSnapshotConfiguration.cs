using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ADHDChecklist.API.Entities;

namespace ADHDChecklist.API.Data.Configurations;

public class AnalyticsSnapshotConfiguration : IEntityTypeConfiguration<AnalyticsSnapshot>
{
    public void Configure(EntityTypeBuilder<AnalyticsSnapshot> builder)
    {
        builder.ToTable("AnalyticsSnapshots");

        // Primary Key
        builder.HasKey(a => a.Id);

        // Properties
        builder.Property(a => a.SnapshotDate)
            .IsRequired();

        builder.Property(a => a.TasksCompleted)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(a => a.TasksCreated)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(a => a.TotalFocusMinutes)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(a => a.HabitsCompleted)
            .IsRequired()
            .HasDefaultValue(0);

        // JSON columns for detailed data
        builder.Property(a => a.HourlyBreakdown)
            .HasColumnType("NVARCHAR(MAX)");

        builder.Property(a => a.CategoryBreakdown)
            .HasColumnType("NVARCHAR(MAX)");

        builder.Property(a => a.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Relationships
        // ✅ FIX: Explicitly configure User relationship
        builder.HasOne(a => a.User)
            .WithMany() // No reverse navigation needed
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint - One snapshot per user per date
        builder.HasIndex(a => new { a.UserId, a.SnapshotDate })
            .IsUnique()
            .HasDatabaseName("IX_AnalyticsSnapshots_UserId_SnapshotDate");

        // Index for range queries
        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("IX_AnalyticsSnapshots_UserId");

        builder.HasIndex(a => a.SnapshotDate)
            .HasDatabaseName("IX_AnalyticsSnapshots_SnapshotDate");
    }
}
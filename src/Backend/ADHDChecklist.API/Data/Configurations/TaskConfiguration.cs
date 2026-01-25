using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ADHDChecklist.API.Data.Configurations;

public class TaskConfiguration : IEntityTypeConfiguration<Entities.Task>
{
    public void Configure(EntityTypeBuilder<Entities.Task> builder)
    {
        builder.ToTable("Tasks");

        // Primary Key
        builder.HasKey(t => t.Id);

        // Properties
        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(1000);

        builder.Property(t => t.CategoryId)
            .IsRequired(false);

        builder.Property(t => t.ScheduledDate)
            .IsRequired();

        builder.Property(t => t.TimeBlockStart)
            .IsRequired(false);

        builder.Property(t => t.TimeBlockEnd)
            .IsRequired(false);

        builder.Property(t => t.Duration)
            .IsRequired(false);

        builder.Property(t => t.IsCompleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.CompletedAt)
            .IsRequired(false);

        builder.Property(t => t.Priority)
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(t => t.IsRecurring)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.RecurrencePattern)
            .HasMaxLength(50);

        builder.Property(t => t.ParentTaskId)
            .IsRequired(false);

        builder.Property(t => t.OrderIndex)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(t => t.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(t => t.DeletedAt)
            .IsRequired(false);

        // Relationships
        builder.HasOne(t => t.User)
            .WithMany(u => u.Tasks)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Category)
            .WithMany(c => c.Tasks)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(t => t.ParentTaskId)
            .IsRequired(false);


        // Indexes for performance
        builder.HasIndex(t => new { t.UserId, t.ScheduledDate })
            .HasDatabaseName("IX_Tasks_UserId_ScheduledDate");

        builder.HasIndex(t => t.IsCompleted)
            .HasDatabaseName("IX_Tasks_IsCompleted");

        builder.HasIndex(t => t.DeletedAt)
            .HasDatabaseName("IX_Tasks_DeletedAt")
            .HasFilter("[DeletedAt] IS NULL");

        builder.HasIndex(t => t.CategoryId)
            .HasDatabaseName("IX_Tasks_CategoryId")
            .HasFilter("[CategoryId] IS NOT NULL");

        builder.HasIndex(t => new { t.UserId, t.CompletedAt })
            .HasDatabaseName("IX_Tasks_UserId_CompletedAt")
            .HasFilter("[CompletedAt] IS NOT NULL");

        // Query filter - Always exclude soft deleted
        builder.HasQueryFilter(t => t.DeletedAt == null);
    }
}
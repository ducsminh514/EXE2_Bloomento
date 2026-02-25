using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ADHDChecklist.API.Entities;

namespace ADHDChecklist.API.Data.Configurations;

public class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> builder)
    {
        builder.ToTable("Reminders");

        // Primary Key
        builder.HasKey(r => r.Id);

        // Properties
        builder.Property(r => r.RemindAt)
            .IsRequired();

        builder.Property(r => r.IsSent)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(r => r.SentAt)
            .IsRequired(false);

        builder.Property(r => r.ReminderType)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("notification");

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("now() at time zone 'utc'");

        // Relationships
        builder.HasOne(r => r.Task)
            .WithMany(t => t.Reminders)
            .HasForeignKey(r => r.TaskId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false); // ? FIX: Make navigation optional

        // Indexes - Important for background job queries
        builder.HasIndex(r => new { r.IsSent, r.RemindAt })
            .HasDatabaseName("IX_Reminders_IsSent_RemindAt")
            .HasFilter("[IsSent] = 0");

        builder.HasIndex(r => r.TaskId)
            .HasDatabaseName("IX_Reminders_TaskId");

        // ? ADD: Query filter matching Task's soft delete
        builder.HasQueryFilter(r => r.Task!.DeletedAt == null);
    }
}

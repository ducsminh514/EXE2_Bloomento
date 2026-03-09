using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ADHDChecklist.API.Entities;

namespace ADHDChecklist.API.Data.Configurations;

public class HabitCompletionConfiguration : IEntityTypeConfiguration<HabitCompletion>
{
    public void Configure(EntityTypeBuilder<HabitCompletion> builder)
    {
        builder.ToTable("HabitCompletions");

        // Primary Key
        builder.HasKey(hc => hc.Id);
        builder.Property(hc => hc.Id)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(hc => hc.CompletionDate)
            .IsRequired();

        builder.Property(hc => hc.CompletedAt)
            .IsRequired()
            .HasDefaultValueSql("now() at time zone 'utc'");

        builder.Property(hc => hc.Notes)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(hc => hc.Habit)
            .WithMany(h => h.HabitCompletions)
            .HasForeignKey(hc => hc.HabitId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint - One completion per habit per day
        builder.HasIndex(hc => new { hc.HabitId, hc.CompletionDate })
            .IsUnique()
            .HasDatabaseName("IX_HabitCompletions_HabitId_CompletionDate");

        // Index for queries
        builder.HasIndex(hc => hc.HabitId)
            .HasDatabaseName("IX_HabitCompletions_HabitId");

        builder.HasIndex(hc => hc.CompletionDate)
            .HasDatabaseName("IX_HabitCompletions_CompletionDate");
    }
}

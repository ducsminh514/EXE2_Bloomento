using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ADHDChecklist.API.Entities;

namespace ADHDChecklist.API.Data.Configurations;

public class BrainDumpItemConfiguration : IEntityTypeConfiguration<BrainDumpItem>
{
    public void Configure(EntityTypeBuilder<BrainDumpItem> builder)
    {
        builder.ToTable("BrainDumpItems");

        // Primary Key
        builder.HasKey(bd => bd.Id);

        // Properties
        builder.Property(bd => bd.Content)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(bd => bd.IsProcessed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(bd => bd.ProcessedAt)
            .IsRequired(false);

        builder.Property(bd => bd.ConvertedToTaskId)
            .IsRequired(false);

        builder.Property(bd => bd.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Relationships
        builder.HasOne(bd => bd.User)
            .WithMany(u => u.BrainDumpItems)
            .HasForeignKey(bd => bd.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(bd => bd.ConvertedToTask)
            .WithMany()
            .HasForeignKey(bd => bd.ConvertedToTaskId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(bd => new { bd.UserId, bd.IsProcessed })
            .HasDatabaseName("IX_BrainDumpItems_UserId_IsProcessed");

        builder.HasIndex(bd => bd.UserId)
            .HasDatabaseName("IX_BrainDumpItems_UserId");

        builder.HasIndex(bd => new { bd.UserId, bd.CreatedAt })
            .HasDatabaseName("IX_BrainDumpItems_UserId_CreatedAt");
    }
}
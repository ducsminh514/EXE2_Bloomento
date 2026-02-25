using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ADHDChecklist.API.Entities;

namespace ADHDChecklist.API.Data.Configurations;

public class DistractionLogConfiguration : IEntityTypeConfiguration<DistractionLog>
{
    public void Configure(EntityTypeBuilder<DistractionLog> builder)
    {
        builder.ToTable("DistractionLogs");

        // Primary Key
        builder.HasKey(dl => dl.Id);

        // Properties
        builder.Property(dl => dl.LoggedAt)
            .IsRequired()
            .HasDefaultValueSql("now() at time zone 'utc'");

        builder.Property(dl => dl.DistractionType)
            .HasMaxLength(100);

        builder.Property(dl => dl.Notes)
            .HasMaxLength(200);

        // Relationships
        builder.HasOne(dl => dl.FocusSession)
            .WithMany(fs => fs.DistractionLogs)
            .HasForeignKey(dl => dl.FocusSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(dl => dl.FocusSessionId)
            .HasDatabaseName("IX_DistractionLogs_FocusSessionId");

        builder.HasIndex(dl => dl.DistractionType)
            .HasDatabaseName("IX_DistractionLogs_DistractionType")
            .HasFilter("[DistractionType] IS NOT NULL");
    }
}

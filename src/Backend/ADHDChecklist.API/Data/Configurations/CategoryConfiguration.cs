using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ADHDChecklist.API.Entities;

namespace ADHDChecklist.API.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        // Primary Key
        builder.HasKey(c => c.Id);

        // Properties
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.ColorHex)
            .IsRequired()
            .HasMaxLength(7)
            .HasDefaultValue("#6B7280");

        builder.Property(c => c.Icon)
            .HasMaxLength(50);

        builder.Property(c => c.OrderIndex)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("now() at time zone 'utc'");

        // Relationships
        builder.HasOne(c => c.User)
            .WithMany(u => u.Categories)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(c => new { c.UserId, c.OrderIndex })
            .HasDatabaseName("IX_Categories_UserId_OrderIndex");

        builder.HasIndex(c => c.UserId)
            .HasDatabaseName("IX_Categories_UserId");
    }
}

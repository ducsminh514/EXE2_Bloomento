using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ADHDChecklist.API.Entities;

namespace ADHDChecklist.API.Data.Configurations;

public class PetTemplateConfiguration : IEntityTypeConfiguration<PetTemplate>
{
    public void Configure(EntityTypeBuilder<PetTemplate> builder)
    {
        builder.ToTable("PetTemplates");

        builder.HasKey(pt => pt.Id);

        builder.Property(pt => pt.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(pt => pt.Species)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(pt => pt.Description)
            .HasMaxLength(500);

        builder.Property(pt => pt.CreatedBy)
            .HasDefaultValue("System");

        builder.Property(pt => pt.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("now() at time zone 'utc'");
    }
}

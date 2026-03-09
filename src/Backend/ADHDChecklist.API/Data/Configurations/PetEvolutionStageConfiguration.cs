using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ADHDChecklist.API.Entities;

namespace ADHDChecklist.API.Data.Configurations;

public class PetEvolutionStageConfiguration : IEntityTypeConfiguration<PetEvolutionStage>
{
    public void Configure(EntityTypeBuilder<PetEvolutionStage> builder)
    {
        builder.ToTable("PetEvolutionStages");

        builder.HasKey(pes => pes.Id);

        builder.Property(pes => pes.EvolutionName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(pes => pes.AssetUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasOne(pes => pes.Template)
            .WithMany(pt => pt.EvolutionStages)
            .HasForeignKey(pes => pes.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

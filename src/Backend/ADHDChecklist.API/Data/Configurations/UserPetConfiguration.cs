using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ADHDChecklist.API.Entities;
using ADHDChecklist.API.Entities.Enums;

namespace ADHDChecklist.API.Data.Configurations;

public class UserPetConfiguration : IEntityTypeConfiguration<UserPet>
{
    public void Configure(EntityTypeBuilder<UserPet> builder)
    {
        builder.ToTable("UserPets");

        builder.HasKey(up => up.Id);
        builder.Property(up => up.Id)
            .ValueGeneratedOnAdd();

        builder.Property(up => up.CustomName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(up => up.TotalXp)
            .HasDefaultValue(0);

        builder.Property(up => up.CurrentLevel)
            .HasDefaultValue(1);

        builder.Property(up => up.CurrentHealth)
            .HasDefaultValue(100);

        builder.Property(up => up.State)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(up => up.LastHpUpdateAt)
            .IsRequired()
            .HasDefaultValueSql("now() at time zone 'utc'");

        builder.Property(up => up.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("now() at time zone 'utc'");

        // Concurrency Token: Removed problematic IsRowVersion for Postgres
        // We keep it as a regular property to satisfy NOT NULL constraints without SQL Server-specific logic
        builder.Property(up => up.RowVersion);

        // One-to-One with ApplicationUser
        builder.HasOne(up => up.User)
            .WithOne(u => u.UserPet)
            .HasForeignKey<UserPet>(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Many-to-One with PetTemplate
        builder.HasOne(up => up.Template)
            .WithMany(pt => pt.UserPets)
            .HasForeignKey(up => up.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ADHDChecklist.API.Entities;

namespace ADHDChecklist.API.Data.Configurations;

public class UserInventoryConfiguration : IEntityTypeConfiguration<UserInventory>
{
    public void Configure(EntityTypeBuilder<UserInventory> builder)
    {
        builder.ToTable("UserInventories");

        builder.HasKey(ui => ui.Id);
        builder.Property(ui => ui.Id)
            .ValueGeneratedOnAdd();

        builder.Property(ui => ui.Coins)
            .HasDefaultValue(0);

        builder.Property(ui => ui.ResurrectionPotionCount)
            .HasDefaultValue(0);

        builder.Property(ui => ui.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("now() at time zone 'utc'");

        // One-to-One with ApplicationUser
        builder.HasOne(ui => ui.User)
            .WithOne(u => u.UserInventory)
            .HasForeignKey<UserInventory>(ui => ui.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

using ADHDChecklist.API.Entities;
using ADHDChecklist.API.Entities.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ADHDChecklist.API.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // Table name - ?? set trong OnModelCreating
        // builder.ToTable("Users");

        // Properties
        builder.Property(u => u.FullName)
            .HasMaxLength(100);

        builder.Property(u => u.SubscriptionTier)
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(SubscriptionTier.Free);

        builder.Property(u => u.SubscriptionExpiry)
            .IsRequired(false);

        builder.Property(u => u.IsEmailVerified)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.EmailVerificationToken)
            .HasMaxLength(100);

        builder.Property(u => u.EmailVerificationTokenExpiry)
            .IsRequired(false);

        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("now() at time zone 'utc'");

        builder.Property(u => u.LastLoginAt)
            .IsRequired(false);

        builder.Property(u => u.TimeZone)
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue("UTC");

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(u => u.GoogleId)
            .HasMaxLength(100);

        builder.Property(u => u.GoogleProfilePicture)
            .HasMaxLength(500);


        // Indexes
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        builder.HasIndex(u => u.GoogleId)
            .HasDatabaseName("IX_Users_GoogleId")
            .HasFilter("\"GoogleId\" IS NOT NULL");


        builder.HasIndex(u => u.IsActive)
            .HasDatabaseName("IX_Users_IsActive");
    }
}

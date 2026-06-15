using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(u => u.VerificationToken).HasMaxLength(256);
        builder.Property(u => u.PasswordResetToken).HasMaxLength(256);
        builder.Property(u => u.RefreshTokenHash).HasMaxLength(256);
        builder.Property(u => u.Status).HasConversion<string>().HasMaxLength(20);
    }
}

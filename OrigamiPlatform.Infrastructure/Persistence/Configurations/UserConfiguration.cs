using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Constants;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    internal static readonly Guid AdminId = SystemUsers.OfficialTutorialAuthorId;
    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

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

        // Seed: admin@origami.com / Admin@123456
        builder.HasData(new User
        {
            Id = AdminId,
            Email = "admin@origami.com",
            PasswordHash = "$2a$11$oJ4bYk4funN7ZPGnjXt0c.q3Or1/y/8TtCQjNMLkmDHNleEkPuGA6",
            Status = AccountStatus.Active,
            CreatedAt = SeedDate
        });
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.HasKey(p => p.UserId);

        builder.Property(p => p.DisplayName).HasMaxLength(100);
        builder.Property(p => p.Bio).HasMaxLength(500);
        builder.Property(p => p.AvatarUrl).HasMaxLength(512);
        builder.Property(p => p.SkillPoints).IsRequired().HasDefaultValue(0);
        builder.Property(p => p.SkillLevel).HasConversion<string>().HasMaxLength(20).IsRequired()
            .HasDefaultValue(SkillLevel.Beginner);

        builder.HasOne(p => p.User)
               .WithOne(u => u.Profile)
               .HasForeignKey<UserProfile>(p => p.UserId);

        builder.HasData(new UserProfile
        {
            UserId = UserConfiguration.AdminId,
            DisplayName = "Admin",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}

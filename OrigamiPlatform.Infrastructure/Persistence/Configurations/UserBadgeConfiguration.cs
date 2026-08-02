using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class UserBadgeConfiguration : IEntityTypeConfiguration<UserBadge>
{
    public void Configure(EntityTypeBuilder<UserBadge> builder)
    {
        builder.HasKey(ub => ub.Id);
        builder.Property(ub => ub.Id).ValueGeneratedNever();

        // BR-35: a user earns each badge at most once
        builder.HasIndex(ub => new { ub.UserId, ub.BadgeId }).IsUnique();

        builder.HasOne(ub => ub.User)
               .WithMany()
               .HasForeignKey(ub => ub.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ub => ub.Badge)
               .WithMany(b => b.UserBadges)
               .HasForeignKey(ub => ub.BadgeId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
{
    public void Configure(EntityTypeBuilder<Wishlist> builder)
    {
        // Composite PK: one wishlist entry per user per target
        builder.HasKey(w => new { w.UserId, w.TargetType, w.TargetId });
        builder.Property(w => w.TargetType).HasConversion<string>().HasMaxLength(20);

        // Same rationale as Like: the PK leads with UserId and can't serve per-target
        // wishlist-count aggregates.
        builder.HasIndex(w => new { w.TargetType, w.TargetId });

        builder.HasOne(w => w.User)
               .WithMany()
               .HasForeignKey(w => w.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

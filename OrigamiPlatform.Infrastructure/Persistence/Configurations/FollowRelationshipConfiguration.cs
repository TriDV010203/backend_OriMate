using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class FollowRelationshipConfiguration : IEntityTypeConfiguration<FollowRelationship>
{
    public void Configure(EntityTypeBuilder<FollowRelationship> builder)
    {
        // Composite PK prevents duplicate follows
        builder.HasKey(f => new { f.FollowerId, f.FollowingId });

        // Follower → User.Following: the follower's list of people they follow
        builder.HasOne(f => f.Follower)
               .WithMany(u => u.Following)
               .HasForeignKey(f => f.FollowerId)
               .OnDelete(DeleteBehavior.Restrict);

        // Following → User.Followers: the followed user's list of their followers
        builder.HasOne(f => f.Following)
               .WithMany(u => u.Followers)
               .HasForeignKey(f => f.FollowingId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class LikeConfiguration : IEntityTypeConfiguration<Like>
{
    public void Configure(EntityTypeBuilder<Like> builder)
    {
        // Composite PK: one like per user per target — enforces uniqueness at DB level
        builder.HasKey(l => new { l.UserId, l.TargetType, l.TargetId });
        // FT-34: widened from 20 to fit "DailyChallengeSubmission" (24 chars)
        builder.Property(l => l.TargetType).HasConversion<string>().HasMaxLength(32);

        // The PK leads with UserId, so it can't serve per-target aggregate queries (like counts
        // for a list of tutorials/posts). This covers those without touching UserId.
        builder.HasIndex(l => new { l.TargetType, l.TargetId });

        builder.HasOne(l => l.User)
               .WithMany()
               .HasForeignKey(l => l.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

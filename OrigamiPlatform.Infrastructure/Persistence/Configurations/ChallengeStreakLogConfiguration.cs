using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class ChallengeStreakLogConfiguration : IEntityTypeConfiguration<ChallengeStreakLog>
{
    public void Configure(EntityTypeBuilder<ChallengeStreakLog> builder)
    {
        builder.HasKey(s => s.UserId);

        builder.Property(s => s.CurrentStreak).IsRequired().HasDefaultValue(0);
        builder.Property(s => s.LongestStreak).IsRequired().HasDefaultValue(0);
        builder.Property(s => s.FreezeCount).IsRequired().HasDefaultValue(0);

        builder.HasOne(s => s.User)
               .WithOne()
               .HasForeignKey<ChallengeStreakLog>(s => s.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

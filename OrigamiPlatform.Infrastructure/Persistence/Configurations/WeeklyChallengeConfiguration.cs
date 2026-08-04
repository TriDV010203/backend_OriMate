using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class WeeklyChallengeConfiguration : IEntityTypeConfiguration<WeeklyChallenge>
{
    public void Configure(EntityTypeBuilder<WeeklyChallenge> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Title).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Theme).HasMaxLength(100);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        // BR: ensure no overlapping weekly challenges if required, but for now we just index by StartDate
        builder.HasIndex(c => c.StartDate);

        builder.HasOne(c => c.Tutorial)
               .WithMany()
               .HasForeignKey(c => c.TutorialId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.CreatedByUser)
               .WithMany()
               .HasForeignKey(c => c.CreatedByUserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Submissions)
               .WithOne(s => s.WeeklyChallenge)
               .HasForeignKey(s => s.WeeklyChallengeId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

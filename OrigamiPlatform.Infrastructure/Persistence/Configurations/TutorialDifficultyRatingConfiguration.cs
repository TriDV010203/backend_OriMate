using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class TutorialDifficultyRatingConfiguration : IEntityTypeConfiguration<TutorialDifficultyRating>
{
    public void Configure(EntityTypeBuilder<TutorialDifficultyRating> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        // One rating per user per tutorial, insert-only.
        builder.HasIndex(r => new { r.UserId, r.TutorialId }).IsUnique();

        builder.HasOne(r => r.User)
               .WithMany()
               .HasForeignKey(r => r.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Tutorial)
               .WithMany()
               .HasForeignKey(r => r.TutorialId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

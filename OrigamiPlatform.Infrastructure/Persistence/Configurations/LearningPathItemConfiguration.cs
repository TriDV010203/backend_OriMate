using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class LearningPathItemConfiguration : IEntityTypeConfiguration<LearningPathItem>
{
    public void Configure(EntityTypeBuilder<LearningPathItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        // A tutorial appears at most once within the same path.
        builder.HasIndex(i => new { i.LearningPathId, i.TutorialId }).IsUnique();

        builder.HasOne(i => i.Tutorial)
               .WithMany()
               .HasForeignKey(i => i.TutorialId)
               .OnDelete(DeleteBehavior.Restrict);
        // Relationship with LearningPath configured in LearningPathConfiguration
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class LearningPathCompletionConfiguration : IEntityTypeConfiguration<LearningPathCompletion>
{
    public void Configure(EntityTypeBuilder<LearningPathCompletion> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        // A user can only complete each learning path once.
        builder.HasIndex(c => new { c.UserId, c.LearningPathId }).IsUnique();

        builder.HasOne(c => c.User)
               .WithMany()
               .HasForeignKey(c => c.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.LearningPath)
               .WithMany()
               .HasForeignKey(c => c.LearningPathId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

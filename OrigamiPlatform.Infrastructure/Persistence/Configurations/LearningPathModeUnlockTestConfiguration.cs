using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class LearningPathModeUnlockTestConfiguration : IEntityTypeConfiguration<LearningPathModeUnlockTest>
{
    public void Configure(EntityTypeBuilder<LearningPathModeUnlockTest> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Instructions).HasMaxLength(1000);

        // Only one active test config per mode.
        builder.HasIndex(t => t.LearningPathModeId).IsUnique();

        builder.HasOne(t => t.LearningPathMode)
               .WithOne(m => m.UnlockTest)
               .HasForeignKey<LearningPathModeUnlockTest>(t => t.LearningPathModeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Tutorial)
               .WithMany()
               .HasForeignKey(t => t.TutorialId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

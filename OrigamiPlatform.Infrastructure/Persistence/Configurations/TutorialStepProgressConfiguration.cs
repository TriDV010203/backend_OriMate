using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class TutorialStepProgressConfiguration : IEntityTypeConfiguration<TutorialStepProgress>
{
    public void Configure(EntityTypeBuilder<TutorialStepProgress> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        // Each user can complete a given step only once.
        builder.HasIndex(p => new { p.UserId, p.TutorialStepId }).IsUnique();

        builder.HasOne(p => p.User)
               .WithMany()
               .HasForeignKey(p => p.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Tutorial)
               .WithMany()
               .HasForeignKey(p => p.TutorialId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.TutorialStep)
               .WithMany()
               .HasForeignKey(p => p.TutorialStepId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

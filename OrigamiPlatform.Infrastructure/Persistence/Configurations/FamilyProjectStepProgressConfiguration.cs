using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class FamilyProjectStepProgressConfiguration : IEntityTypeConfiguration<FamilyProjectStepProgress>
{
    public void Configure(EntityTypeBuilder<FamilyProjectStepProgress> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.HasOne(p => p.Project)
               .WithMany(fp => fp.StepProgresses)
               .HasForeignKey(p => p.ProjectId)
               .OnDelete(DeleteBehavior.Cascade);

        // BR-39: project completion requires ≥ 1 progress record per step
        builder.HasOne(p => p.Step)
               .WithMany(s => s.StepProgresses)
               .HasForeignKey(p => p.StepId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.CompletedByUser)
               .WithMany()
               .HasForeignKey(p => p.CompletedBy)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

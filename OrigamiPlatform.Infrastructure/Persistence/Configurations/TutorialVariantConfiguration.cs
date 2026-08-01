using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class TutorialVariantConfiguration : IEntityTypeConfiguration<TutorialVariant>
{
    public void Configure(EntityTypeBuilder<TutorialVariant> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();

        // Don't link the same pair twice
        builder.HasIndex(v => new { v.ParentTutorialId, v.VariantTutorialId }).IsUnique();

        builder.HasOne(v => v.ParentTutorial)
               .WithMany()
               .HasForeignKey(v => v.ParentTutorialId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.VariantTutorial)
               .WithMany()
               .HasForeignKey(v => v.VariantTutorialId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class TutorialStepConfiguration : IEntityTypeConfiguration<TutorialStep>
{
    public void Configure(EntityTypeBuilder<TutorialStep> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Description).HasMaxLength(4000).IsRequired();
        builder.Property(s => s.ImageUrl).HasMaxLength(512);
        // Relationship with Tutorial configured in TutorialConfiguration
    }
}

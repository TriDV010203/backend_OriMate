using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class LearningPathModeConfiguration : IEntityTypeConfiguration<LearningPathMode>
{
    public void Configure(EntityTypeBuilder<LearningPathMode> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Name).HasMaxLength(100).IsRequired();
        builder.Property(m => m.Description).HasMaxLength(500);

        builder.HasIndex(m => m.SortOrder).IsUnique();

        // LearningPath ↔ LearningPathMode relationship configured in LearningPathConfiguration
    }
}

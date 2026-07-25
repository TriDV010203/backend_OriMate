using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class LearningPathConfiguration : IEntityTypeConfiguration<LearningPath>
{
    public void Configure(EntityTypeBuilder<LearningPath> builder)
    {
        builder.HasKey(lp => lp.Id);
        builder.Property(lp => lp.Id).ValueGeneratedNever();

        builder.Property(lp => lp.Title).HasMaxLength(150).IsRequired();
        builder.Property(lp => lp.Description).HasMaxLength(1000).IsRequired();
        builder.Property(lp => lp.CoverImageUrl).HasMaxLength(512);
        builder.Property(lp => lp.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne(lp => lp.CreatedByUser)
               .WithMany()
               .HasForeignKey(lp => lp.CreatedByUserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(lp => lp.Items)
               .WithOne(i => i.LearningPath)
               .HasForeignKey(i => i.LearningPathId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

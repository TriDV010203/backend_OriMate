using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class TutorialReviewHistoryConfiguration : IEntityTypeConfiguration<TutorialReviewHistory>
{
    public void Configure(EntityTypeBuilder<TutorialReviewHistory> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.ReviewerRole).HasConversion<string>();
        builder.Property(r => r.FromStatus).HasConversion<string>();
        builder.Property(r => r.ToStatus).HasConversion<string>();
        builder.Property(r => r.Action).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Reason).HasMaxLength(1000);

        // Tutorial relationship configured in TutorialConfiguration
        builder.HasOne(r => r.Reviewer)
               .WithMany()
               .HasForeignKey(r => r.ReviewerId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class ModeUnlockSubmissionConfiguration : IEntityTypeConfiguration<ModeUnlockSubmission>
{
    public void Configure(EntityTypeBuilder<ModeUnlockSubmission> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.PhotoUrl).HasMaxLength(512).IsRequired();
        builder.Property(s => s.Note).HasMaxLength(1000);
        builder.Property(s => s.ReviewNote).HasMaxLength(1000);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(s => new { s.UserId, s.LearningPathModeId, s.Status });

        builder.HasOne(s => s.User)
               .WithMany()
               .HasForeignKey(s => s.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.LearningPathMode)
               .WithMany()
               .HasForeignKey(s => s.LearningPathModeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Tutorial)
               .WithMany()
               .HasForeignKey(s => s.TutorialId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.ReviewedByUser)
               .WithMany()
               .HasForeignKey(s => s.ReviewedByUserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

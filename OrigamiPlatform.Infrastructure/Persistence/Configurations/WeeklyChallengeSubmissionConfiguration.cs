using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class WeeklyChallengeSubmissionConfiguration : IEntityTypeConfiguration<WeeklyChallengeSubmission>
{
    public void Configure(EntityTypeBuilder<WeeklyChallengeSubmission> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.PhotoUrl).HasMaxLength(1000).IsRequired();
        builder.Property(s => s.Note).HasMaxLength(500);

        // One submission per user per weekly challenge
        builder.HasIndex(s => new { s.WeeklyChallengeId, s.UserId }).IsUnique();

        builder.HasOne(s => s.User)
               .WithMany()
               .HasForeignKey(s => s.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class DailyChallengeSubmissionConfiguration : IEntityTypeConfiguration<DailyChallengeSubmission>
{
    public void Configure(EntityTypeBuilder<DailyChallengeSubmission> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.PhotoUrl).HasMaxLength(512).IsRequired();
        builder.Property(s => s.Note).HasMaxLength(500);

        // BR-34: one submission per user per challenge
        builder.HasIndex(s => new { s.DailyChallengeId, s.UserId }).IsUnique();

        builder.HasOne(s => s.User)
               .WithMany()
               .HasForeignKey(s => s.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

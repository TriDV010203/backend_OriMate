using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class PersonalMilestoneConfiguration : IEntityTypeConfiguration<PersonalMilestone>
{
    public void Configure(EntityTypeBuilder<PersonalMilestone> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        // A user can only unlock each threshold once.
        builder.HasIndex(m => new { m.UserId, m.Threshold }).IsUnique();

        builder.HasOne(m => m.User)
               .WithMany()
               .HasForeignKey(m => m.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

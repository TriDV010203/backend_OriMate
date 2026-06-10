using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class FamilyProjectConfiguration : IEntityTypeConfiguration<FamilyProject>
{
    public void Configure(EntityTypeBuilder<FamilyProject> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>();

        builder.HasOne(p => p.Owner)
               .WithMany()
               .HasForeignKey(p => p.OwnerId)
               .OnDelete(DeleteBehavior.Restrict);

        // BR-28: only Free tutorials eligible for FamilyProject
        builder.HasOne(p => p.Tutorial)
               .WithMany()
               .HasForeignKey(p => p.TutorialId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Subscription)
               .WithMany(s => s.Projects)
               .HasForeignKey(p => p.SubscriptionId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

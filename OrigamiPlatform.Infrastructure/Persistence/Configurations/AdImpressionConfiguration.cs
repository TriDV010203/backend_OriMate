using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class AdImpressionConfiguration : IEntityTypeConfiguration<AdImpression>
{
    public void Configure(EntityTypeBuilder<AdImpression> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.IpAddress).HasMaxLength(45);
        builder.Property(i => i.Cost).HasColumnType("decimal(18,4)").IsRequired();

        builder.HasOne(i => i.Campaign)
               .WithMany(c => c.Impressions)
               .HasForeignKey(i => i.CampaignId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Banner)
               .WithMany()
               .HasForeignKey(i => i.BannerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Step)
               .WithMany()
               .HasForeignKey(i => i.StepId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.User)
               .WithMany()
               .HasForeignKey(i => i.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

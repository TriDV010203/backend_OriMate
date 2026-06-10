using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class AdBannerConfiguration : IEntityTypeConfiguration<AdBanner>
{
    public void Configure(EntityTypeBuilder<AdBanner> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.ImageUrl).HasMaxLength(512).IsRequired();
        builder.Property(b => b.AltText).HasMaxLength(200);

        builder.HasOne(b => b.Campaign)
               .WithMany(c => c.Banners)
               .HasForeignKey(b => b.CampaignId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

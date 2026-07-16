using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class ShopLinkConfiguration : IEntityTypeConfiguration<ShopLink>
{
    public void Configure(EntityTypeBuilder<ShopLink> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Title).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Url).HasMaxLength(500).IsRequired();
        builder.Property(s => s.ImageUrl).HasMaxLength(500);
        builder.Property(s => s.Category).HasMaxLength(100);
    }
}

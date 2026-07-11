using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class CreatorVipSettingsConfiguration : IEntityTypeConfiguration<CreatorVipSettings>
{
    public void Configure(EntityTypeBuilder<CreatorVipSettings> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        // BR-13: creator must have active VIP settings to publish VIP tutorials
        builder.Property(s => s.Price).HasColumnType("decimal(18,2)").IsRequired();

        builder.HasIndex(s => s.CreatorId).IsUnique();

        builder.HasOne(s => s.Creator)
               .WithMany()
               .HasForeignKey(s => s.CreatorId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

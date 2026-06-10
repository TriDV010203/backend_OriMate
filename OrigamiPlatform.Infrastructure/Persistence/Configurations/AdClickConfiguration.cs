using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class AdClickConfiguration : IEntityTypeConfiguration<AdClick>
{
    public void Configure(EntityTypeBuilder<AdClick> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.IpAddress).HasMaxLength(45);
        builder.Property(c => c.Cost).HasColumnType("decimal(18,4)").IsRequired();

        builder.HasOne(c => c.Campaign)
               .WithMany(a => a.Clicks)
               .HasForeignKey(c => c.CampaignId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Banner)
               .WithMany()
               .HasForeignKey(c => c.BannerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Step)
               .WithMany()
               .HasForeignKey(c => c.StepId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.User)
               .WithMany()
               .HasForeignKey(c => c.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

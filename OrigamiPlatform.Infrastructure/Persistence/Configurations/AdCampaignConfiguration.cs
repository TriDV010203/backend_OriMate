using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class AdCampaignConfiguration : IEntityTypeConfiguration<AdCampaign>
{
    public void Configure(EntityTypeBuilder<AdCampaign> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.DestinationUrl).HasMaxLength(512).IsRequired();
        builder.Property(c => c.RejectionReason).HasMaxLength(500);
        builder.Property(c => c.PricingType).HasConversion<string>().HasMaxLength(10);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.RatePerUnit).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(c => c.TotalBudget).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(c => c.BudgetRemaining).HasColumnType("decimal(18,2)").IsRequired();

        builder.HasOne(c => c.Partner)
               .WithMany()
               .HasForeignKey(c => c.PartnerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Placement)
               .WithMany(p => p.Campaigns)
               .HasForeignKey(c => c.PlacementId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ApprovedByUser)
               .WithMany()
               .HasForeignKey(c => c.ApprovedBy)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

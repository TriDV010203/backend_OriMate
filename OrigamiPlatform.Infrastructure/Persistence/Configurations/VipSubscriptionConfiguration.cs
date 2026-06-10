using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class VipSubscriptionConfiguration : IEntityTypeConfiguration<VipSubscription>
{
    public void Configure(EntityTypeBuilder<VipSubscription> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();

        builder.Property(v => v.Status).HasConversion<string>();

        builder.HasOne(v => v.Subscriber)
               .WithMany(u => u.VipSubscriptions)
               .HasForeignKey(v => v.SubscriberId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Creator)
               .WithMany()
               .HasForeignKey(v => v.CreatorId)
               .OnDelete(DeleteBehavior.Restrict);

        // BR-24: 30-day fixed, one transaction per subscription
        builder.HasOne(v => v.Transaction)
               .WithMany()
               .HasForeignKey(v => v.TransactionId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

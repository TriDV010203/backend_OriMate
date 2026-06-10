using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class FamilySubscriptionConfiguration : IEntityTypeConfiguration<FamilySubscription>
{
    public void Configure(EntityTypeBuilder<FamilySubscription> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(s => s.User)
               .WithMany()
               .HasForeignKey(s => s.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Transaction)
               .WithMany()
               .HasForeignKey(s => s.TransactionId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class SePayWebhookLogConfiguration : IEntityTypeConfiguration<SePayWebhookLog>
{
    public void Configure(EntityTypeBuilder<SePayWebhookLog> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.Gateway).HasMaxLength(100).IsRequired();
        builder.Property(l => l.TransferAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(l => l.Content).HasMaxLength(500).IsRequired();
        builder.Property(l => l.MatchResult).HasConversion<string>().HasMaxLength(30);
        builder.Property(l => l.RawPayload).HasColumnType("nvarchar(max)").IsRequired();

        builder.HasIndex(l => l.SePayTransactionId).IsUnique();

        builder.HasOne(l => l.Transaction)
               .WithMany()
               .HasForeignKey(l => l.TransactionId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}

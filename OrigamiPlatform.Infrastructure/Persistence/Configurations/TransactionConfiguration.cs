using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Amount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(t => t.PlatformFeeAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(t => t.CreatorNetAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(t => t.TransactionType).HasConversion<string>().HasMaxLength(30);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(t => t.ReferenceCode).HasMaxLength(100);
        builder.Property(t => t.AdminNote).HasMaxLength(300);

        builder.HasOne(t => t.User)
               .WithMany()
               .HasForeignKey(t => t.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ConfirmedByUser)
               .WithMany()
               .HasForeignKey(t => t.ConfirmedBy)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Creator)
               .WithMany()
               .HasForeignKey(t => t.CreatorId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

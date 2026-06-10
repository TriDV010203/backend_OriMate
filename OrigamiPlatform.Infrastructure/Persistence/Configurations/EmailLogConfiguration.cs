using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class EmailLogConfiguration : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.ToEmail).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Subject).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Type).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();

        builder.HasOne(e => e.Recipient)
               .WithMany()
               .HasForeignKey(e => e.RecipientId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

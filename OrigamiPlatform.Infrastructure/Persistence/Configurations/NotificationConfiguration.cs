using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();

        builder.Property(n => n.Type).HasConversion<string>();
        builder.Property(n => n.Message).HasMaxLength(500).IsRequired();
        builder.Property(n => n.EntityType).HasMaxLength(100);

        builder.HasOne(n => n.Recipient)
               .WithMany()
               .HasForeignKey(n => n.RecipientId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

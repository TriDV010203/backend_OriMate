using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class JournalConfiguration : IEntityTypeConfiguration<Journal>
{
    public void Configure(EntityTypeBuilder<Journal> builder)
    {
        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).ValueGeneratedNever();

        builder.Property(j => j.Content).HasMaxLength(4000).IsRequired();
        builder.Property(j => j.ImageUrls).HasMaxLength(2000);

        builder.HasOne(j => j.User)
               .WithMany()
               .HasForeignKey(j => j.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(j => j.LinkedTutorial)
               .WithMany()
               .HasForeignKey(j => j.LinkedTutorialId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

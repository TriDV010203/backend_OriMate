using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class ClanConfiguration : IEntityTypeConfiguration<Clan>
{
    public void Configure(EntityTypeBuilder<Clan> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();

        builder.HasIndex(c => c.Name).IsUnique();

        builder.HasOne(c => c.Owner)
               .WithMany()
               .HasForeignKey(c => c.OwnerId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

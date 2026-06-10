using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class BlockedWordConfiguration : IEntityTypeConfiguration<BlockedWord>
{
    public void Configure(EntityTypeBuilder<BlockedWord> builder)
    {
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedOnAdd();

        builder.Property(w => w.Word).HasMaxLength(100).IsRequired();
        builder.HasIndex(w => w.Word).IsUnique();
    }
}

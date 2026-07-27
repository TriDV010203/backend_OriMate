using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class BadgeConfiguration : IEntityTypeConfiguration<Badge>
{
    public void Configure(EntityTypeBuilder<Badge> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.Code).HasMaxLength(50).IsRequired();
        builder.Property(b => b.Name).HasMaxLength(150).IsRequired();
        builder.Property(b => b.Description).HasMaxLength(300).IsRequired();
        builder.Property(b => b.IconEmoji).HasMaxLength(10).IsRequired();
        builder.Property(b => b.Category).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(b => b.DifficultyFilter).HasConversion<string>().HasMaxLength(50);
        builder.Property(b => b.IsActive).HasDefaultValue(true);

        builder.HasIndex(b => b.Code).IsUnique();
    }
}

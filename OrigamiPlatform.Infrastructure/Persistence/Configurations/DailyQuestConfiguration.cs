using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class DailyQuestConfiguration : IEntityTypeConfiguration<DailyQuest>
{
    public void Configure(EntityTypeBuilder<DailyQuest> builder)
    {
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id).ValueGeneratedNever();

        builder.Property(q => q.Title).IsRequired().HasMaxLength(200);
        builder.Property(q => q.IsActive).IsRequired().HasDefaultValue(true);
    }
}

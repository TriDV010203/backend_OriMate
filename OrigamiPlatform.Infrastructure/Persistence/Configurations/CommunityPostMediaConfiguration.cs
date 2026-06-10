using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class CommunityPostMediaConfiguration : IEntityTypeConfiguration<CommunityPostMedia>
{
    public void Configure(EntityTypeBuilder<CommunityPostMedia> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.MediaType).HasConversion<string>();
        builder.Property(m => m.Url).HasMaxLength(512).IsRequired();

        builder.HasOne(m => m.Post)
               .WithMany(p => p.Media)
               .HasForeignKey(m => m.PostId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

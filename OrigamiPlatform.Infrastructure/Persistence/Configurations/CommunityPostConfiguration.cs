using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class CommunityPostConfiguration : IEntityTypeConfiguration<CommunityPost>
{
    public void Configure(EntityTypeBuilder<CommunityPost> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Content).HasMaxLength(2000).IsRequired();

        // Covers the feed's Where(IsVisible, IsDeleted) + OrderByDescending(CreatedAt).
        builder.HasIndex(p => new { p.IsVisible, p.IsDeleted, p.CreatedAt });

        builder.HasOne(p => p.Author)
               .WithMany()
               .HasForeignKey(p => p.AuthorId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.LinkedTutorial)
               .WithMany()
               .HasForeignKey(p => p.LinkedTutorialId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

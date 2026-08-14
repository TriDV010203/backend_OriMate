using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.TargetType).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Content).HasMaxLength(2000).IsRequired();

        // Serves both the top-level lookup (TargetType=Tutorial/CommunityPost, TargetId=<entity>)
        // and the reply lookup (TargetType=Comment, TargetId=<parent comment>) used by comment counts.
        builder.HasIndex(c => new { c.TargetType, c.TargetId });

        builder.HasOne(c => c.Author)
               .WithMany()
               .HasForeignKey(c => c.AuthorId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

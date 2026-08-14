using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class TutorialConfiguration : IEntityTypeConfiguration<Tutorial>
{
    public void Configure(EntityTypeBuilder<Tutorial> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Title).HasMaxLength(150).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(500).IsRequired();
        builder.Property(t => t.Slug).HasMaxLength(120).IsRequired();
        builder.HasIndex(t => t.Slug).IsUnique();

        builder.Property(t => t.CoverImageUrl).HasMaxLength(512);
        builder.Property(t => t.MetaTitle).HasMaxLength(160);
        builder.Property(t => t.MetaDescription).HasMaxLength(320);
        builder.Property(t => t.Tags).HasMaxLength(300);
        builder.Property(t => t.Difficulty).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(t => t.Type).HasConversion<string>().HasMaxLength(10);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(t => t.IsOfficial).HasDefaultValue(false);

        // Covers the public tutorial list's Where(Status, IsDeleted) + OrderByDescending(PublishedAt) —
        // the default (non-"likes") sort path.
        builder.HasIndex(t => new { t.Status, t.IsDeleted, t.PublishedAt });

        builder.HasOne(t => t.Author)
               .WithMany(u => u.Tutorials)
               .HasForeignKey(t => t.AuthorId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Category)
               .WithMany(c => c.Tutorials)
               .HasForeignKey(t => t.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);

        // self-referencing: revised tutorial derived from a parent
        builder.HasOne(t => t.ParentTutorial)
               .WithMany(t => t.ChildTutorials)
               .HasForeignKey(t => t.ParentTutorialId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Steps)
               .WithOne(s => s.Tutorial)
               .HasForeignKey(s => s.TutorialId)
               .OnDelete(DeleteBehavior.Cascade);

        // BR-17: history is INSERT-only; Restrict prevents accidental cascade delete
        builder.HasMany(t => t.ReviewHistories)
               .WithOne(r => r.Tutorial)
               .HasForeignKey(r => r.TutorialId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

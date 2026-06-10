using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class AchievementConfiguration : IEntityTypeConfiguration<Achievement>
{
    public void Configure(EntityTypeBuilder<Achievement> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.PhotoUrl).HasMaxLength(512);
        builder.Property(a => a.Note).HasMaxLength(1000);

        // BR-33: one achievement record per user per tutorial
        builder.HasIndex(a => new { a.UserId, a.TutorialId }).IsUnique();

        builder.HasOne(a => a.User)
               .WithMany(u => u.Achievements)
               .HasForeignKey(a => a.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Tutorial)
               .WithMany(t => t.Achievements)
               .HasForeignKey(a => a.TutorialId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class UserPaperPatternConfiguration : IEntityTypeConfiguration<UserPaperPattern>
{
    public void Configure(EntityTypeBuilder<UserPaperPattern> builder)
    {
        builder.HasKey(up => up.Id);
        builder.Property(up => up.Id).ValueGeneratedNever();

        // Don't buy the same pattern twice
        builder.HasIndex(up => new { up.UserId, up.PaperPatternId }).IsUnique();

        builder.HasOne(up => up.User)
               .WithMany()
               .HasForeignKey(up => up.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(up => up.PaperPattern)
               .WithMany()
               .HasForeignKey(up => up.PaperPatternId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

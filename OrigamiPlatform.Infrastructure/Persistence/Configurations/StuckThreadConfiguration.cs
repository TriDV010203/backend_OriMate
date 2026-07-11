using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class StuckThreadConfiguration : IEntityTypeConfiguration<StuckThread>
{
    public void Configure(EntityTypeBuilder<StuckThread> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.HasOne(s => s.Tutorial)
               .WithMany()
               .HasForeignKey(s => s.TutorialId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Step)
               .WithMany()
               .HasForeignKey(s => s.StepId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.User)
               .WithMany()
               .HasForeignKey(s => s.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

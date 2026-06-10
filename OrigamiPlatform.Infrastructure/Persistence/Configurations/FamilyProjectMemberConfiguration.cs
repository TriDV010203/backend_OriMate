using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class FamilyProjectMemberConfiguration : IEntityTypeConfiguration<FamilyProjectMember>
{
    public void Configure(EntityTypeBuilder<FamilyProjectMember> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Status).HasConversion<string>();

        builder.HasOne(m => m.Project)
               .WithMany(p => p.Members)
               .HasForeignKey(m => m.ProjectId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.User)
               .WithMany()
               .HasForeignKey(m => m.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

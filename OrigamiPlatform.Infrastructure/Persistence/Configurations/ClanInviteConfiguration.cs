using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class ClanInviteConfiguration : IEntityTypeConfiguration<ClanInvite>
{
    public void Configure(EntityTypeBuilder<ClanInvite> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        // Not unique: a user can be invited again after a previous invite expired.
        builder.HasIndex(i => new { i.ClanId, i.UserId });

        builder.HasOne(i => i.Clan)
               .WithMany()
               .HasForeignKey(i => i.ClanId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.User)
               .WithMany()
               .HasForeignKey(i => i.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

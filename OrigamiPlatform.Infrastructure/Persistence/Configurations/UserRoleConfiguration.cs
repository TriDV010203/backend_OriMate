using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.HasKey(r => new { r.UserId, r.Role });
        builder.Property(r => r.Role).HasConversion<string>().HasMaxLength(30);

        builder.HasOne(r => r.User)
               .WithMany(u => u.Roles)
               .HasForeignKey(r => r.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            new UserRole { UserId = UserConfiguration.AdminId, Role = UserRoleType.User,  CreatedAt = seedDate },
            new UserRole { UserId = UserConfiguration.AdminId, Role = UserRoleType.Admin, CreatedAt = seedDate }
        );
    }
}

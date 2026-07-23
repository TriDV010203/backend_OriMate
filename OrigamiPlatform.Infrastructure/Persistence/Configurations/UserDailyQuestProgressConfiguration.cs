using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Configurations;

public class UserDailyQuestProgressConfiguration : IEntityTypeConfiguration<UserDailyQuestProgress>
{
    public void Configure(EntityTypeBuilder<UserDailyQuestProgress> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        // Reset ngầm định theo ngày — chặn tạo trùng row cùng quest cùng ngày cho 1 user.
        builder.HasIndex(p => new { p.UserId, p.QuestId, p.QuestDate }).IsUnique();

        builder.HasOne(p => p.User)
               .WithMany()
               .HasForeignKey(p => p.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Quest)
               .WithMany()
               .HasForeignKey(p => p.QuestId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

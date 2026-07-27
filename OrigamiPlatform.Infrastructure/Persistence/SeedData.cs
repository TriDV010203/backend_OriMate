// OrigamiPlatform.Infrastructure/Persistence/SeedData.cs
//
// Seed dữ liệu tối thiểu để môi trường dev chạy được end-to-end.
// KHÔNG chạy tự động trong Production — chỉ gọi từ Program.cs khi
// app.Environment.IsDevelopment(), xem hướng dẫn ở docs/SEED_DATA.md.

using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Infrastructure.Persistence;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext context, IPasswordHasher passwordHasher)
    {
        await SeedCategoriesAsync(context);
        await SeedBlockedWordsAsync(context);
        await SeedAdminAccountsAsync(context, passwordHasher);
        await SeedDailyQuestsAsync(context);
        await SeedBadgesAsync(context);
        await context.SaveChangesAsync();
    }

    private static async Task SeedCategoriesAsync(AppDbContext context)
    {
        if (await context.Categories.AnyAsync()) return;

        var now = DateTime.UtcNow;
        var categories = new[]
        {
            new Category { Name = "Động vật", IsActive = true, CreatedAt = now },
            new Category { Name = "Hoa & Thực vật", IsActive = true, CreatedAt = now },
            new Category { Name = "Đồ vật & Hộp", IsActive = true, CreatedAt = now },
            new Category { Name = "Nhân vật & Trang phục", IsActive = true, CreatedAt = now },
            new Category { Name = "Kỹ thuật cơ bản", IsActive = true, CreatedAt = now },
        };

        context.Categories.AddRange(categories);
    }

    private static async Task SeedBlockedWordsAsync(AppDbContext context)
    {
        if (await context.BlockedWords.AnyAsync()) return;

        // ⚠️ Đây CHỈ là placeholder để chứng minh cơ chế BlockedWordService hoạt động.
        // Team cần tự bổ sung danh sách từ cấm thật (tiếng Việt + tiếng Anh) trước khi
        // đưa lên môi trường thật — không seed sẵn danh sách từ nhạy cảm ở đây.
        var placeholders = new[] { "spamword1", "spamword2", "testbadword" };
        var now = DateTime.UtcNow;

        context.BlockedWords.AddRange(placeholders.Select(w => new BlockedWord
        {
            Word = w,
            CreatedAt = now
        }));
    }

    private static async Task SeedAdminAccountsAsync(AppDbContext context, IPasswordHasher passwordHasher)
    {
        if (await context.Users.AnyAsync(u => u.Email == "admin@orimate.dev")) return;

        var now = DateTime.UtcNow;

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@orimate.dev",
            PasswordHash = passwordHasher.Hash("Admin@123"), // ⚠️ CHỈ dùng cho dev, đổi ngay nếu deploy thật
            Status = AccountStatus.Active,
            CreatedAt = now,
        };
        var manager = new User
        {
            Id = Guid.NewGuid(),
            Email = "manager@orimate.dev",
            PasswordHash = passwordHasher.Hash("Manager@123"),
            Status = AccountStatus.Active,
            CreatedAt = now,
        };

        context.Users.AddRange(admin, manager);

        context.UserProfiles.AddRange(
            new UserProfile { UserId = admin.Id, DisplayName = "Seed Admin", CreatedAt = now },
            new UserProfile { UserId = manager.Id, DisplayName = "Seed Manager", CreatedAt = now }
        );

        context.UserRoles.AddRange(
            new UserRole { UserId = admin.Id, Role = UserRoleType.Admin, CreatedAt = now },
            new UserRole { UserId = manager.Id, Role = UserRoleType.Manager, CreatedAt = now }
        );
    }

    // FT-27: single fixed Daily Quest for MVP — no Admin CRUD for DailyQuest in this pass.
    private static async Task SeedDailyQuestsAsync(AppDbContext context)
    {
        if (await context.DailyQuests.AnyAsync()) return;

        context.DailyQuests.Add(new DailyQuest
        {
            Id = Guid.NewGuid(),
            Title = "Hoàn thành 3 bước học hôm nay",
            TargetValue = 3,
            IsActive = true
        });
    }

    // FT-35: badge catalog — not admin-editable in v1, awarded automatically via BadgeAwardService
    // from the existing PersonalMilestone/Streak trigger points plus the new Daily Challenge flow.
    private static async Task SeedBadgesAsync(AppDbContext context)
    {
        if (await context.Badges.AnyAsync()) return;

        var now = DateTime.UtcNow;

        Badge Make(string code, string name, string description, string icon, BadgeCategory category, int? threshold = null, TutorialDifficulty? difficultyFilter = null)
            => new()
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = name,
                Description = description,
                IconEmoji = icon,
                Category = category,
                Threshold = threshold,
                DifficultyFilter = difficultyFilter,
                IsActive = true,
                CreatedAt = now
            };

        var badges = new[]
        {
            Make("TUTORIAL_COUNT_10", "Người mới bắt đầu", "Hoàn thành 10 hướng dẫn.", "🌱", BadgeCategory.TutorialCount, 10),
            Make("TUTORIAL_COUNT_30", "Thợ gấp chăm chỉ", "Hoàn thành 30 hướng dẫn.", "🧵", BadgeCategory.TutorialCount, 30),
            Make("TUTORIAL_COUNT_50", "Nghệ nhân", "Hoàn thành 50 hướng dẫn.", "🎨", BadgeCategory.TutorialCount, 50),
            Make("TUTORIAL_COUNT_100", "Bậc thầy Origami", "Hoàn thành 100 hướng dẫn.", "👑", BadgeCategory.TutorialCount, 100),
            Make("DIFFICULTY_ADVANCED_10", "Chinh phục bài Khó", "Hoàn thành 10 hướng dẫn ở mức Khó.", "🔥", BadgeCategory.DifficultyCount, 10, TutorialDifficulty.Advanced),
            Make("STREAK_LEARNING_7", "7 ngày kiên trì", "Duy trì chuỗi học tập 7 ngày liên tiếp.", "📘", BadgeCategory.StreakLearning, 7),
            Make("STREAK_LEARNING_14", "14 ngày bền bỉ", "Duy trì chuỗi học tập 14 ngày liên tiếp.", "📗", BadgeCategory.StreakLearning, 14),
            Make("STREAK_LEARNING_30", "30 ngày huyền thoại", "Duy trì chuỗi học tập 30 ngày liên tiếp.", "📙", BadgeCategory.StreakLearning, 30),
            Make("STREAK_CHALLENGE_3", "Khởi đầu thử thách", "Tham gia Thử thách ngày 3 ngày liên tiếp.", "🔰", BadgeCategory.StreakChallenge, 3),
            Make("STREAK_CHALLENGE_7", "Chiến binh thử thách", "Tham gia Thử thách ngày 7 ngày liên tiếp.", "🏅", BadgeCategory.StreakChallenge, 7),
            Make("STREAK_CHALLENGE_30", "Huyền thoại thử thách", "Tham gia Thử thách ngày 30 ngày liên tiếp.", "🏆", BadgeCategory.StreakChallenge, 30),
            Make("CHALLENGE_RANK1_FIRST", "Quán quân ngày", "Đạt hạng 1 trong một Thử thách ngày.", "🥇", BadgeCategory.ChallengeRank),
            Make("CHALLENGE_TOP3_FIRST", "Vào Top 3", "Lọt Top 3 một Thử thách ngày.", "🥉", BadgeCategory.ChallengeRank, 1),
            Make("CHALLENGE_TOP3_5X", "Top 3 quen mặt", "Lọt Top 3 Thử thách ngày 5 lần.", "⭐", BadgeCategory.ChallengeRank, 5),
            Make("CHALLENGE_AUTHOR_SELECTED", "Tác giả được chọn", "Hướng dẫn của bạn được chọn làm Thử thách ngày.", "✍️", BadgeCategory.Author, 1),
            Make("CHALLENGE_AUTHOR_5X", "Tác giả nổi bật", "Hướng dẫn của bạn được chọn làm Thử thách ngày 5 lần.", "🌟", BadgeCategory.Author, 5),
        };

        await context.Badges.AddRangeAsync(badges);
    }
}

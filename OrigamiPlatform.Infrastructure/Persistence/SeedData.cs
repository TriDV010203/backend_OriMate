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
}

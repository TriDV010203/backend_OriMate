using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.GamificationAndPortfolio;

public class AchievementTests : IntegrationTestBase
{
    public AchievementTests(CustomWebApplicationFactory factory) : base(factory) { }

    // [Happy Path] (FT-19) - Ghi nhận thành tựu hợp lệ, mặc định phải là Private (BR-ACH-02)
    [Fact]
    public async Task CreateAchievement_FirstTime_CreatesSuccessfully_DefaultsToPrivate()
    {
        // 1. Arrange: Khởi tạo dữ liệu nền (Danh mục, Tác giả, và Bài học)
        var (categoryId, authorId) = await SeedDefaultPrerequisitesAsync();
        var tutorialId = Guid.NewGuid();

        _dbContext.Tutorials.Add(new Tutorial
        {
            Id = tutorialId,
            Title = "Hạc giấy cơ bản",
            Slug = "hac-giay-co-ban", // ĐÃ SỬA: Cấp phát Slug giả để không vi phạm Unique Index
            Status = TutorialStatus.Published,
            CategoryId = categoryId,
            AuthorId = authorId
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Đăng nhập với tư cách là người đi học (User)
        var userId = await AuthenticateAsAsync("User");

        // Payload dựa trên CreateAchievementRequest
        var request = new
        {
            TutorialId = tutorialId,
            Note = "Lần đầu tiên gấp thành công con hạc này!",
            ImageUrl = "https://fake-cloudinary.com/hac.jpg",
            IsPublic = false
        };

        // 2. Act: Gọi API tạo Thành tựu
        var response = await _client.PostAsJsonAsync("/api/achievements", request);

        // 3. Assert: Kiểm tra Database
        response.EnsureSuccessStatusCode();

        _dbContext.ChangeTracker.Clear();
        var achievement = await _dbContext.Achievements
            .FirstOrDefaultAsync(a => a.UserId == userId && a.TutorialId == tutorialId);

        achievement.Should().NotBeNull("Hệ thống phải tạo thành công bản ghi Achievement");

        // Cực kỳ quan trọng: Test thiết kế mặc định bảo vệ quyền riêng tư[cite: 1]
        achievement!.IsPublic.Should().BeFalse("Lỗi BE: Theo BR-ACH-02, Achievement mặc định phải là Private để bảo vệ người dùng");
    }

    // [Error Path / Boundary] (FT-19) - Cố tình ghi nhận thành tựu lần 2 cho cùng 1 bài học (BR-ACH-01)
    [Fact]
    public async Task CreateAchievement_DuplicateForSameTutorial_ReturnsBadRequest()
    {
        // 1. Arrange: Tạo sẵn 1 Bài học và 1 Thành tựu đã hoàn thành trước đó của User
        var (categoryId, authorId) = await SeedDefaultPrerequisitesAsync();
        var tutorialId = Guid.NewGuid();

        _dbContext.Tutorials.Add(new Tutorial
        {
            Id = tutorialId,
            Title = "Thuyền giấy",
            Slug = "thuyen-giay", // ĐÃ SỬA: Cấp phát Slug giả
            Status = TutorialStatus.Published,
            CategoryId = categoryId,
            AuthorId = authorId
        });

        var userId = await AuthenticateAsAsync("User");

        // Insert thành tựu lần 1 trực tiếp vào DB
        _dbContext.Achievements.Add(new Achievement
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TutorialId = tutorialId,
            IsPublic = true,
            Note = "Lần 1"
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // 2. Act: Cố tình gọi API để tạo thành tựu lần 2 cho chính bài học đó
        var request = new
        {
            TutorialId = tutorialId,
            Note = "Làm lại lần 2 hi vọng sẽ đẹp hơn",
            ImageUrl = "https://fake-cloudinary.com/thuyen2.jpg"
        };

        var response = await _client.PostAsJsonAsync("/api/achievements", request);

        // 3. Assert: Bắt buộc BE phải chặn lại ở tầng Domain hoặc Application
        // Dựa vào BR-ACH-01 trong tài liệu SRS[cite: 1]
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Lỗi BE: Hệ thống đã vi phạm BR-ACH-01 khi cho phép 1 User tạo 2 Achievements trên cùng 1 Tutorial.");
    }
}
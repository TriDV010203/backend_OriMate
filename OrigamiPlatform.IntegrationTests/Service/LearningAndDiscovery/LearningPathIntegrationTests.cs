using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.LearningPaths;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Controllers.LearningAndDiscovery;

public class LearningPathIntegrationTests : IntegrationTestBase
{
    public LearningPathIntegrationTests(CustomWebApplicationFactory factory) : base(factory) { }

    // 🔬 Coverage Technique: Happy Path (Admin creates and publishes a structured learning path - FT-33)
    [Fact]
    public async Task CreateAndPublishLearningPath_AsAdmin_ReturnsSuccess_HappyPath()
    {
        // Arrange: Đăng nhập Admin và chuẩn bị dữ liệu Category, Mode, Official Tutorial hợp lệ
        var adminId = await AuthenticateAsAsync("Admin");

        var category = new Domain.Entities.Category { Name = "Origami Roadmap", IsActive = true };
        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync();

        var mode = new Domain.Entities.LearningPathMode
        {
            Id = Guid.NewGuid(),
            Name = "Beginner Path",
            SortOrder = 1,
            IsActive = true
        };
        _dbContext.LearningPathModes.Add(mode);
        await _dbContext.SaveChangesAsync();

        var officialTutorial = new Domain.Entities.Tutorial
        {
            Id = Guid.NewGuid(),
            AuthorId = OrigamiPlatform.Domain.Constants.SystemUsers.OfficialTutorialAuthorId,
            CategoryId = category.Id,
            Title = "Official Introduction Fold",
            Description = "An official introductory fold description meeting length rules.",
            Slug = "official-intro-fold",
            CoverImageUrl = "https://img.com/cover.jpg",
            Type = TutorialType.Free,
            Difficulty = TutorialDifficulty.Beginner,
            Status = TutorialStatus.Published,
            IsOfficial = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Tutorials.Add(officialTutorial);
        await _dbContext.SaveChangesAsync();

        var request = new CreateLearningPathRequest(
            Title: "Mastering Basic Folds",
            Description: "A complete structured learning path for beginners starting out.",
            CoverImageUrl: "https://img.com/path.jpg",
            LearningPathModeId: mode.Id,
            TutorialIds: new List<Guid> { officialTutorial.Id }
        );

        // Act 1: Tạo Learning Path ở trạng thái Draft
        var createResponse = await _client.PostAsJsonAsync("/api/learning-paths", request);
        createResponse.EnsureSuccessStatusCode();

        var createdJson = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var pathId = createdJson.GetProperty("id").GetGuid();

        // Act 2: Xuất bản Learning Path
        var publishResponse = await _client.PutAsync($"/api/learning-paths/{pathId}/publish", null);

        // Assert
        publishResponse.EnsureSuccessStatusCode();

        _dbContext.ChangeTracker.Clear();
        var dbPath = await _dbContext.LearningPaths.Include(p => p.Items).FirstAsync(p => p.Id == pathId);
        dbPath.Status.Should().Be(LearningPathStatus.Published);
        dbPath.Items.Count.Should().Be(1);
    }

    // 🔬 Coverage Technique: Error Path (Adding non-official or non-published tutorial is rejected)
    [Fact]
    public async Task CreateLearningPath_WithNonOfficialTutorial_ReturnsBadRequest_ErrorPath()
    {
        // Arrange: Đăng nhập để tạo User hợp lệ làm AuthorId trong DB test
        var authorId = await AuthenticateAsAsync("User");
        await AuthenticateAsAsync("Admin"); // Chuyển về quyền Admin để gọi API tạo Learning Path

        var category = new Domain.Entities.Category { Name = "Advanced Fold", IsActive = true };
        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync();

        var mode = new Domain.Entities.LearningPathMode
        {
            Id = Guid.NewGuid(),
            Name = "Expert Path",
            SortOrder = 2,
            IsActive = true
        };
        _dbContext.LearningPathModes.Add(mode);
        await _dbContext.SaveChangesAsync();

        // Tutorial của user thường (IsOfficial = false) với AuthorId hợp lệ đã tồn tại trong DB
        var userTutorial = new Domain.Entities.Tutorial
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId, // Sử dụng authorId hợp lệ tránh lỗi khóa ngoại Users
            CategoryId = category.Id,
            Title = "User Custom Fold",
            Description = "A custom description meeting requirements.",
            Slug = "user-custom-fold",
            CoverImageUrl = "https://img.com/cover.jpg",
            Type = TutorialType.Free,
            Difficulty = TutorialDifficulty.Advanced,
            Status = TutorialStatus.Published,
            IsOfficial = false,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Tutorials.Add(userTutorial);
        await _dbContext.SaveChangesAsync();

        var request = new CreateLearningPathRequest(
            Title: "Invalid Path Collection",
            Description: "This path tries to include non-official content.",
            CoverImageUrl: "https://img.com/path.jpg",
            LearningPathModeId: mode.Id,
            TutorialIds: new List<Guid> { userTutorial.Id }
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/learning-paths", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
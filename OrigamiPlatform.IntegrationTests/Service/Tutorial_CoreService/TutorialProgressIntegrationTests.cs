using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Services.Tutorial_CoreService;

public class TutorialProgressIntegrationTests : IntegrationTestBase
{
    public TutorialProgressIntegrationTests(CustomWebApplicationFactory factory) : base(factory) { }

    // 🔬 Coverage Technique: Happy Path (User marks tutorial step as complete)
    [Fact]
    public async Task CompleteStep_ValidStep_ReturnsSuccess_AndUpdatesProgress_HappyPath()
    {
        // Arrange: Đăng nhập để tạo User hợp lệ trong DB
        var userId = await AuthenticateAsAsync("User");

        var category = new Domain.Entities.Category { Name = "Origami Animals", IsActive = true };
        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync();

        var tutorialId = Guid.NewGuid();
        var tutorial = new Domain.Entities.Tutorial
        {
            Id = tutorialId,
            AuthorId = userId, // Sử dụng userId hợp lệ làm AuthorId
            CategoryId = category.Id,
            Title = "Progress Test Tutorial",
            Description = "Valid description for progress tracking test.",
            Slug = "progress-test",
            CoverImageUrl = "https://img.com/cover.jpg",
            Type = TutorialType.Free,
            Difficulty = TutorialDifficulty.Beginner,
            Status = TutorialStatus.Published,
            CreatedAt = DateTime.UtcNow
        };

        var stepId = Guid.NewGuid();
        var step = new Domain.Entities.TutorialStep
        {
            Id = stepId,
            TutorialId = tutorialId,
            StepOrder = 1,
            Description = "Fold paper in half.",
            ImageUrl = "https://img.com/step1.jpg"
        };
        tutorial.Steps.Add(step);

        _dbContext.Tutorials.Add(tutorial);
        await _dbContext.SaveChangesAsync();

        // Act: User đánh dấu hoàn thành bước
        var response = await _client.PostAsync($"/api/tutorials/{tutorialId}/steps/{stepId}/complete", null);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("completedStepIds").GetArrayLength().Should().Be(1);

        _dbContext.ChangeTracker.Clear();
        var progress = await _dbContext.TutorialStepProgresses.FirstOrDefaultAsync(p => p.UserId == userId && p.TutorialStepId == stepId);
        progress.Should().NotBeNull();
    }

    // 🔬 Coverage Technique: Idempotency & Happy Path (Raising stuck flag returns existing thread if already raised - FT-10)
    [Fact]
    public async Task RaiseStuckFlag_FirstTime_CreatesThread_SecondTime_ReturnsExisting_Idempotency()
    {
        // Arrange: Đăng nhập User hợp lệ
        var userId = await AuthenticateAsAsync("User");

        var category = new Domain.Entities.Category { Name = "Origami Flowers", IsActive = true };
        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync();

        var tutorialId = Guid.NewGuid();
        var tutorial = new Domain.Entities.Tutorial
        {
            Id = tutorialId,
            AuthorId = userId, // Sử dụng userId hợp lệ làm AuthorId
            CategoryId = category.Id,
            Title = "Stuck Flag Test",
            Description = "Valid description for stuck flag test.",
            Slug = "stuck-flag-test",
            CoverImageUrl = "https://img.com/cover.jpg",
            Type = TutorialType.Free,
            Difficulty = TutorialDifficulty.Intermediate,
            Status = TutorialStatus.Published,
            CreatedAt = DateTime.UtcNow
        };

        var stepId = Guid.NewGuid();
        var step = new Domain.Entities.TutorialStep
        {
            Id = stepId,
            TutorialId = tutorialId,
            StepOrder = 3,
            Description = "Tricky reverse fold.",
            ImageUrl = "https://img.com/step3.jpg"
        };
        tutorial.Steps.Add(step);

        _dbContext.Tutorials.Add(tutorial);
        await _dbContext.SaveChangesAsync();

        // Act: Gọi lần 1 (Tạo mới StuckThread)
        var response1 = await _client.PostAsync($"/api/tutorials/{tutorialId}/steps/{stepId}/stuck", null);
        response1.EnsureSuccessStatusCode();
        var result1 = await response1.Content.ReadFromJsonAsync<JsonElement>();
        var threadId1 = result1.GetProperty("id").GetGuid();

        // Act: Gọi lần 2 (Kiểm tra tính bất biến / idempotent - trả về đúng thread cũ)
        var response2 = await _client.PostAsync($"/api/tutorials/{tutorialId}/steps/{stepId}/stuck", null);
        response2.EnsureSuccessStatusCode();
        var result2 = await response2.Content.ReadFromJsonAsync<JsonElement>();
        var threadId2 = result2.GetProperty("id").GetGuid();

        // Assert
        threadId1.Should().Be(threadId2);

        _dbContext.ChangeTracker.Clear();
        var stuckCount = await _dbContext.StuckThreads.CountAsync(st => st.UserId == userId && st.StepId == stepId);
        stuckCount.Should().Be(1, "Không được phép tạo trùng lặp StuckThread cho cùng một bước");
    }
}
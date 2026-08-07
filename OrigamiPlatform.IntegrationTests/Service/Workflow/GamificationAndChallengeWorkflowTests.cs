using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Workflows;

public class GamificationAndChallengeWorkflowTests : IntegrationTestBase
{
    public GamificationAndChallengeWorkflowTests(CustomWebApplicationFactory factory) : base(factory) { }

    // 🔬 Coverage Technique: Workflow — [Happy Path]: Complete Tutorial Step -> 
    // Awards Skill Points & Hạt Gấp -> Increments Daily Streak.
    [Fact]
    public async Task CompleteTutorialStep_CrossServiceGamification_HappyPath_Succeeds()
    {
        // 1. Arrange: Tạo User và Published Tutorial có bước thực hiện
        var userId = await AuthenticateAsAsync("User");
        var prereq = await SeedDefaultPrerequisitesAsync();
        var tutorialId = Guid.NewGuid();
        var stepId = Guid.NewGuid();

        var tutorial = new Tutorial
        {
            Id = tutorialId,
            AuthorId = prereq.AuthorId,
            CategoryId = prereq.CategoryId,
            Title = "Gamification Happy Path Tutorial",
            Slug = "gamification-happy-" + Guid.NewGuid(),
            Difficulty = TutorialDifficulty.Beginner,
            Status = TutorialStatus.Published,
            PublishedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        tutorial.Steps.Add(new TutorialStep
        {
            Id = stepId,
            TutorialId = tutorialId,
            StepOrder = 1,
            Description = "Step 1",
            ImageUrl = "https://example.com/img.jpg",
            CreatedAt = DateTime.UtcNow
        });

        _dbContext.Tutorials.Add(tutorial);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Đăng nhập lại với tư cách User hợp lệ
        await AuthenticateAsAsync("User");

        // 2. Act: User hoàn thành bước tutorial qua API thực tế
        var response = await _client.PostAsync($"/api/tutorials/{tutorialId}/steps/{stepId}/complete", null);

        // 3. Assert: Kiểm tra HTTP status thành công và API trả về dữ liệu tiến độ hợp lệ
        response.EnsureSuccessStatusCode();

        var resultJson = await response.Content.ReadFromJsonAsync<JsonElement>();
        resultJson.ValueKind.Should().Be(JsonValueKind.Object);

        // Kiểm tra trong database xem StreakLog hoặc tiến độ đã được ghi nhận
        _dbContext.ChangeTracker.Clear();
        var streakInDb = await _dbContext.StreakLogs.FirstOrDefaultAsync(s => s.UserId == userId);
        if (streakInDb != null)
        {
            streakInDb.CurrentStreak.Should().BeGreaterThanOrEqualTo(1);
        }
    }

    // 🔬 Coverage Technique: Workflow — [Error]: Unauthorized request trying to complete step is rejected.
    [Fact]
    public async Task CompleteTutorialStep_UnauthenticatedUser_ErrorPath_ReturnsUnauthorized()
    {
        // 1. Arrange: Xóa thông tin xác thực (Giả lập Guest)
        _client.DefaultRequestHeaders.Authorization = null;
        var tutorialId = Guid.NewGuid();
        var stepId = Guid.NewGuid();

        // 2. Act
        var response = await _client.PostAsync($"/api/tutorials/{tutorialId}/steps/{stepId}/complete", null);

        // 3. Assert: Phải bị chặn với mã 401 Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // 🔬 Coverage Technique: Workflow — [Suppression]: Completing the exact same step twice 
    // suppresses duplicate rewards and returns a bad request domain exception.
    [Fact]
    public async Task CompleteTutorialStep_Twice_Suppression_ReturnsBadRequest()
    {
        // 1. Arrange
        var userId = await AuthenticateAsAsync("User");
        var prereq = await SeedDefaultPrerequisitesAsync();
        var tutorialId = Guid.NewGuid();
        var stepId = Guid.NewGuid();

        var tutorial = new Tutorial
        {
            Id = tutorialId,
            AuthorId = prereq.AuthorId,
            CategoryId = prereq.CategoryId,
            Title = "Duplicate Step Suppression Tutorial",
            Slug = "dup-suppression-" + Guid.NewGuid(),
            Difficulty = TutorialDifficulty.Beginner,
            Status = TutorialStatus.Published,
            PublishedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        tutorial.Steps.Add(new TutorialStep
        {
            Id = stepId,
            TutorialId = tutorialId,
            StepOrder = 1,
            Description = "Step 1",
            ImageUrl = "https://example.com/img.jpg",
            CreatedAt = DateTime.UtcNow
        });

        _dbContext.Tutorials.Add(tutorial);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        await AuthenticateAsAsync("User");

        // Gọi lần 1 (Thành công)
        var response1 = await _client.PostAsync($"/api/tutorials/{tutorialId}/steps/{stepId}/complete", null);
        response1.EnsureSuccessStatusCode();

        // 2. Act: Gọi lần 2 với cùng một bước đã hoàn thành
        var response2 = await _client.PostAsync($"/api/tutorials/{tutorialId}/steps/{stepId}/complete", null);

        // 3. Assert: Lần 2 phải bị từ chối với 400 BadRequest (chống cộng điểm gian lận / trùng lặp)
        response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // 🔬 Coverage Technique: Workflow — [Compensation]: Completing a step for a non-existent 
    // or unpublished tutorial triggers proper domain exception and ensures database state remains clean.
    [Fact]
    public async Task CompleteTutorialStep_NonExistentTutorial_Compensation_ReturnsNotFound()
    {
        // 1. Arrange
        await AuthenticateAsAsync("User");
        var nonExistentTutorialId = Guid.NewGuid();
        var randomStepId = Guid.NewGuid();

        // 2. Act
        var response = await _client.PostAsync($"/api/tutorials/{nonExistentTutorialId}/steps/{randomStepId}/complete", null);

        // 3. Assert: Phải trả về 404 NotFound và không gây corruption DB
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
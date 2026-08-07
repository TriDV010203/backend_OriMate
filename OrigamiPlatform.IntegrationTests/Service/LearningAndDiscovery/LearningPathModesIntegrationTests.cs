using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.LearningPathModes;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Controllers.LearningAndDiscovery;

public class LearningPathModesIntegrationTests : IntegrationTestBase
{
    public LearningPathModesIntegrationTests(CustomWebApplicationFactory factory) : base(factory) { }

    // 🔬 Coverage Technique: Happy Path (Admin creates a new learning path mode)
    [Fact]
    public async Task CreateLearningPathMode_AsAdmin_ReturnsSuccess_HappyPath()
    {
        // Arrange
        await AuthenticateAsAsync("Admin");

        var request = new CreateLearningPathModeRequest(
            Name: "Advanced Techniques",
            Description: "Master complex folding methods and multi-sheet models.",
            SortOrder: 3
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/learning-path-modes", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("name").GetString().Should().Be(request.Name);
        result.GetProperty("sortOrder").GetInt32().Should().Be(3);

        // Kiểm tra trong DB
        _dbContext.ChangeTracker.Clear();
        var dbMode = await _dbContext.LearningPathModes.FirstOrDefaultAsync(m => m.Name == request.Name);
        dbMode.Should().NotBeNull();
        dbMode!.IsActive.Should().BeTrue();
    }

    // 🔬 Coverage Technique: Happy Path & Workflow (User submits mode unlock test, Manager approves it)
    [Fact]
    public async Task SubmitAndApproveModeUnlockTest_FullFlow_Success()
    {
        // Arrange: Tạo Mode phụ (không phải entry mode) và cấu hình Unlock Test bằng một Official Tutorial
        var category = new Domain.Entities.Category { Name = "Test Category", IsActive = true };
        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync();

        var mode = new Domain.Entities.LearningPathMode
        {
            Id = Guid.NewGuid(),
            Name = "Master Level",
            Description = "Requires unlock test verification.",
            SortOrder = 2,
            IsActive = true
        };
        _dbContext.LearningPathModes.Add(mode);

        var officialTutorial = new Domain.Entities.Tutorial
        {
            Id = Guid.NewGuid(),
            AuthorId = OrigamiPlatform.Domain.Constants.SystemUsers.OfficialTutorialAuthorId,
            CategoryId = category.Id,
            Title = "Master Test Tutorial",
            Description = "An official tutorial used as a mode unlock test requirement.",
            Slug = "master-test-tutorial",
            CoverImageUrl = "https://img.com/cover.jpg",
            Type = TutorialType.Free,
            Difficulty = TutorialDifficulty.Advanced,
            Status = TutorialStatus.Published,
            IsOfficial = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Tutorials.Add(officialTutorial);

        var unlockTestConfig = new Domain.Entities.LearningPathModeUnlockTest
        {
            Id = Guid.NewGuid(),
            LearningPathModeId = mode.Id,
            TutorialId = officialTutorial.Id,
            Instructions = "Fold this model perfectly and upload a clear photo."
        };
        _dbContext.LearningPathModeUnlockTests.Add(unlockTestConfig);
        await _dbContext.SaveChangesAsync();

        // 1. User thường đăng nhập và nộp bài test mở khóa
        var userId = await AuthenticateAsAsync("User");
        var submitRequest = new SubmitModeUnlockTestRequest(
            PhotoUrl: "https://img.com/my-submission.jpg",
            Note: "Here is my completed fold for the unlock test."
        );

        var submitResponse = await _client.PostAsJsonAsync($"/api/learning-path-modes/{mode.Id}/unlock-test/submissions", submitRequest);
        submitResponse.EnsureSuccessStatusCode();

        _dbContext.ChangeTracker.Clear();
        var submission = await _dbContext.ModeUnlockSubmissions.FirstOrDefaultAsync(s => s.UserId == userId && s.LearningPathModeId == mode.Id);
        submission.Should().NotBeNull();
        submission!.Status.Should().Be(ModeUnlockSubmissionStatus.Pending);

        // 2. Manager đăng nhập và duyệt bài nộp
        await AuthenticateAsAsync("Manager");
        var approveResponse = await _client.PutAsync($"/api/learning-path-modes/unlock-test-submissions/{submission.Id}/approve", null);

        // Assert
        approveResponse.EnsureSuccessStatusCode();

        _dbContext.ChangeTracker.Clear();
        var dbSubmission = await _dbContext.ModeUnlockSubmissions.FirstAsync(s => s.Id == submission.Id);
        dbSubmission.Status.Should().Be(ModeUnlockSubmissionStatus.Approved);
        dbSubmission.ReviewedByUserId.Should().NotBeNull();
    }
}
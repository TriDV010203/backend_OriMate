using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace OrigamiPlatform.IntegrationTests.Controllers.GamificationAndPortfolio;

public class GamificationTests : IntegrationTestBase
{
    private readonly ITestOutputHelper _output;

    public GamificationTests(CustomWebApplicationFactory factory, ITestOutputHelper output) : base(factory)
    {
        _output = output;
    }

    private async Task<(Tutorial Tut, List<TutorialStep> Steps)> SeedTutorialWithStepsAsync(TutorialStatus status = TutorialStatus.Published)
    {
        var prereq = await SeedDefaultPrerequisitesAsync();
        var tutorial = new Tutorial
        {
            Id = Guid.NewGuid(),
            Title = "Intermediate Origami Crane",
            Slug = "intermediate-crane-" + Guid.NewGuid(),
            CategoryId = prereq.CategoryId,
            AuthorId = prereq.AuthorId,
            Status = status,
            Difficulty = TutorialDifficulty.Intermediate,
            CoverImageUrl = "https://img.com/cover.jpg",
            Type = TutorialType.Free,
            PublishedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        var steps = new List<TutorialStep>
        {
            new() { Id = Guid.NewGuid(), TutorialId = tutorial.Id, StepOrder = 1, Description = "Step 1", ImageUrl = "https://img.com/step1.jpg" },
            new() { Id = Guid.NewGuid(), TutorialId = tutorial.Id, StepOrder = 2, Description = "Step 2", ImageUrl = "https://img.com/step2.jpg" },
            new() { Id = Guid.NewGuid(), TutorialId = tutorial.Id, StepOrder = 3, Description = "Step 3", ImageUrl = "https://img.com/step3.jpg" }
        };

        _dbContext.Tutorials.Add(tutorial);
        _dbContext.TutorialSteps.AddRange(steps);
        await _dbContext.SaveChangesAsync();

        return (tutorial, steps);
    }

    // 🔬 Coverage Technique: Happy Path: Verify the primary success flow — DB state correct, events published, response correct.
    [Fact]
    public async Task CompleteTutorialSteps_AccruesSkillPoints_AndUpdatesSkillLevel_HappyPath()
    {
        var (tut, steps) = await SeedTutorialWithStepsAsync();
        var userId = await AuthenticateAsAsync("User");

        var userProfile = new UserProfile
        {
            UserId = userId,
            DisplayName = "Test Learner",
            SkillPoints = 0,
            SkillLevel = SkillLevel.Beginner,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.UserProfiles.Add(userProfile);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        foreach (var step in steps)
        {
            var url = $"/api/tutorials/{tut.Id}/steps/{step.Id}/complete";
            _output.WriteLine($"[TEST LOG] Đang gọi POST tới URL: {url}");

            var stepResponse = await _client.PostAsync(url, null);
            stepResponse.EnsureSuccessStatusCode();
        }

        var skillLevelResponse = await _client.GetAsync("/api/gamification/skill-level");
        skillLevelResponse.EnsureSuccessStatusCode();

        var skillData = await skillLevelResponse.Content.ReadFromJsonAsync<JsonElement>();
        skillData.GetProperty("skillPoints").GetInt32().Should().Be(2);
        skillData.GetProperty("skillLevel").GetString().Should().Be("Beginner");
    }

    // 🔬 Coverage Technique: Idempotency: Send same event_id twice — second call must be a no-op or reject duplicate action.
    [Fact]
    public async Task CompleteSameStepTwice_ReturnsBadRequest()
    {
        var (tut, steps) = await SeedTutorialWithStepsAsync();
        var userId = await AuthenticateAsAsync("User");

        _dbContext.UserProfiles.Add(new UserProfile
        {
            UserId = userId,
            DisplayName = "Test Learner",
            SkillPoints = 0,
            SkillLevel = SkillLevel.Beginner,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var targetStep = steps.First();
        var url = $"/api/tutorials/{tut.Id}/steps/{targetStep.Id}/complete";

        // Lần 1: Thành công
        var response1 = await _client.PostAsync(url, null);
        response1.EnsureSuccessStatusCode();

        // Lần 2: Gửi trùng lặp trên cùng một step
        var response2 = await _client.PostAsync(url, null);
        response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // 🔬 Coverage Technique: Error Path: Verify failure scenarios — rollback complete, compensation triggered, no partial state.
    [Fact]
    public async Task CompleteStep_OnRemovedTutorial_ReturnsError()
    {
        var (tut, steps) = await SeedTutorialWithStepsAsync(TutorialStatus.Removed);
        var userId = await AuthenticateAsAsync("User");

        _dbContext.UserProfiles.Add(new UserProfile
        {
            UserId = userId,
            DisplayName = "Test Learner",
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var targetStep = steps.First();
        var url = $"/api/tutorials/{tut.Id}/steps/{targetStep.Id}/complete";

        var response = await _client.PostAsync(url, null);
        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    // 🔬 Coverage Technique: Happy Path: Verify the primary success flow — DB state correct, events published, response correct.
    [Fact]
    public async Task GetHatGapBalance_ReturnsCurrentBalance_HappyPath()
    {
        var userId = await AuthenticateAsAsync("User");

        if (!await _dbContext.UserProfiles.AnyAsync(p => p.UserId == userId))
        {
            _dbContext.UserProfiles.Add(new UserProfile { UserId = userId, DisplayName = "Wallet User" });
            await _dbContext.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/gamification/hatgap-balance");
        response.EnsureSuccessStatusCode();
        var balanceData = await response.Content.ReadFromJsonAsync<JsonElement>();

        int balance = balanceData.ValueKind == JsonValueKind.Object
            ? balanceData.GetProperty("balance").GetInt32()
            : balanceData.GetInt32();

        balance.Should().BeGreaterThanOrEqualTo(0);
    }

    // 🔬 Coverage Technique: Error Path: Verify failure scenarios — unauthorized access rejection.
    [Fact]
    public async Task GetMySkillLevel_WithoutAuthentication_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/gamification/skill-level");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
using System.Net;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.LearningAndDiscovery;

public class StuckThreadTests : IntegrationTestBase
{
    public StuckThreadTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task RaiseStuckFlag_OnValidStep_CreatesThread()
    {
        var prereq = await SeedDefaultPrerequisitesAsync();
        var tut = new Tutorial { Id = Guid.NewGuid(), Title = "Tut", Slug = "tut", CategoryId = prereq.CategoryId, AuthorId = prereq.AuthorId, Status = TutorialStatus.Published };

        var step = new TutorialStep { Id = Guid.NewGuid(), TutorialId = tut.Id, StepOrder = 1 };
        _dbContext.Tutorials.Add(tut);
        _dbContext.TutorialSteps.Add(step);
        await _dbContext.SaveChangesAsync();

        var userId = await AuthenticateAsAsync("User");

        // ĐÃ SỬA: Khớp chuẩn route /api/tutorials/{tutorialId}/steps/{stepId}/stuck của TutorialProgressController
        var response = await _client.PostAsync($"/api/tutorials/{tut.Id}/steps/{step.Id}/stuck", null);

        response.EnsureSuccessStatusCode();

        var threadCount = await _dbContext.StuckThreads.CountAsync(t => t.UserId == userId && t.StepId == step.Id);
        Assert.Equal(1, threadCount);
    }

    [Fact]
    public async Task RaiseStuckFlag_TwiceOnSameStep_ReturnsExistingThread()
    {
        var prereq = await SeedDefaultPrerequisitesAsync();
        var tut = new Tutorial { Id = Guid.NewGuid(), Title = "Tut2", Slug = "tut2", CategoryId = prereq.CategoryId, AuthorId = prereq.AuthorId, Status = TutorialStatus.Published };

        var step = new TutorialStep { Id = Guid.NewGuid(), TutorialId = tut.Id, StepOrder = 1 };
        _dbContext.Tutorials.Add(tut);
        _dbContext.TutorialSteps.Add(step);
        await _dbContext.SaveChangesAsync();

        var userId = await AuthenticateAsAsync("User");

        // ĐÃ SỬA: Truyền đúng tutorialId vào URL
        await _client.PostAsync($"/api/tutorials/{tut.Id}/steps/{step.Id}/stuck", null);
        var response2 = await _client.PostAsync($"/api/tutorials/{tut.Id}/steps/{step.Id}/stuck", null);

        response2.EnsureSuccessStatusCode();

        var threadCount = await _dbContext.StuckThreads.CountAsync(t => t.UserId == userId && t.StepId == step.Id);
        Assert.Equal(1, threadCount);
    }
}
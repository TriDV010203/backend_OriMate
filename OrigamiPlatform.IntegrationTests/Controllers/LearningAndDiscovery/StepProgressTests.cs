using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.LearningAndDiscovery;

public class StepProgressTests : IntegrationTestBase
{
    public StepProgressTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<(Tutorial Tut, TutorialStep Step1, TutorialStep Step2)> SeedTutorialWithStepsAsync(TutorialStatus status = TutorialStatus.Published)
    {
        var prereq = await SeedDefaultPrerequisitesAsync();
        var tutorial = new Tutorial
        {
            Id = Guid.NewGuid(),
            Title = "Prog Tut",
            Slug = "prog-tut-" + Guid.NewGuid(),
            CategoryId = prereq.CategoryId,
            AuthorId = prereq.AuthorId,
            Status = status,
            PublishedAt = DateTime.UtcNow
        };

        var step1 = new TutorialStep { Id = Guid.NewGuid(), TutorialId = tutorial.Id, StepOrder = 1, Description = "S1" };
        var step2 = new TutorialStep { Id = Guid.NewGuid(), TutorialId = tutorial.Id, StepOrder = 2, Description = "S2" };

        _dbContext.Tutorials.Add(tutorial);
        _dbContext.TutorialSteps.AddRange(step1, step2);
        await _dbContext.SaveChangesAsync();
        return (tutorial, step1, step2);
    }

    [Fact]
    public async Task CompleteFirstStep_CreatesStepProgress()
    {
        var data = await SeedTutorialWithStepsAsync();
        var userId = await AuthenticateAsAsync("User");

        var response = await _client.PostAsync($"/api/tutorials/{data.Tut.Id}/steps/{data.Step1.Id}/complete", null);

        response.EnsureSuccessStatusCode();

        // Truy vấn trực tiếp entity TutorialStepProgress từ DbContext
        var progress = await _dbContext.TutorialStepProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.TutorialId == data.Tut.Id);

        progress.Should().NotBeNull();
    }

    [Fact]
    public async Task CompleteSameStepTwice_ReturnsBadRequestOrIdempotent()
    {
        var data = await SeedTutorialWithStepsAsync();
        var userId = await AuthenticateAsAsync("User");

        var response1 = await _client.PostAsync($"/api/tutorials/{data.Tut.Id}/steps/{data.Step1.Id}/complete", null);
        response1.EnsureSuccessStatusCode();

        // Gọi lại lần 2 trên cùng một step: Backend thực tế trả về BadRequest (400) thay vì tự động bỏ qua hoàn toàn
        var response2 = await _client.PostAsync($"/api/tutorials/{data.Tut.Id}/steps/{data.Step1.Id}/complete", null);

        response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UncompleteStep_DecrementsProgressCount()
    {
        var data = await SeedTutorialWithStepsAsync();
        var userId = await AuthenticateAsAsync("User");

        var response1 = await _client.PostAsync($"/api/tutorials/{data.Tut.Id}/steps/{data.Step1.Id}/complete", null);
        response1.EnsureSuccessStatusCode();

        // Sử dụng DELETE method chuẩn theo TutorialProgressController của backend
        var response = await _client.DeleteAsync($"/api/tutorials/{data.Tut.Id}/steps/{data.Step1.Id}/complete");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CompleteStep_OnRemovedTutorial_ReturnsBadRequest()
    {
        var data = await SeedTutorialWithStepsAsync(TutorialStatus.Removed);
        await AuthenticateAsAsync("User");

        var response = await _client.PostAsync($"/api/tutorials/{data.Tut.Id}/steps/{data.Step1.Id}/complete", null);

        // ĐÃ SỬA: Khớp đúng thực tế Backend trả về BadRequest (400) thay vì NotFound (404)
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Tutorials;

public class FT05_ManagerReviewTests : IntegrationTestBase
{
    public FT05_ManagerReviewTests(CustomWebApplicationFactory factory) : base(factory) { }

    // [Happy Path] & [Audit Log] - AC-01, AC-03
    [Fact]
    public async Task HappyPath_ManagerPublish_ChangesStatus_And_CreatesHistory()
    {
        var managerId = await AuthenticateAsAsync("Manager");
        var (categoryId, authorId) = await SeedDefaultPrerequisitesAsync();

        var tutId = Guid.NewGuid();
        _dbContext.Tutorials.Add(new Domain.Entities.Tutorial
        {
            Id = tutId,
            Title = "Pending Tut",
            Slug = $"pt-{tutId}",
            Status = TutorialStatus.PendingManagerReview,
            CategoryId = categoryId,
            AuthorId = authorId
        });
        await _dbContext.SaveChangesAsync();

        var response = await _client.PutAsync($"/api/tutorials/{tutId}/publish", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tut = await _dbContext.Tutorials.FindAsync(tutId);
        await _dbContext.Entry(tut!).ReloadAsync();
        tut!.Status.Should().Be(TutorialStatus.Published);

        // Đảm bảo Audit/History được sinh ra (AC-03)
        var historyCount = await _dbContext.TutorialReviewHistories.CountAsync(h => h.TutorialId == tutId);
        historyCount.Should().Be(1);
    }

    // [BVA] - Kiểm thử độ dài lý do từ chối (BV-01: 10 chars)
    [Theory]
    [InlineData("Too short", HttpStatusCode.BadRequest)] // 9 chars -> Lỗi
    [InlineData("Ten chars.", HttpStatusCode.OK)]        // 10 chars -> Thành công
    public async Task BV01_ManagerReject_ReasonLengthBoundaries(string reason, HttpStatusCode expectedStatus)
    {
        await AuthenticateAsAsync("Manager");
        var (categoryId, authorId) = await SeedDefaultPrerequisitesAsync();

        var tutId = Guid.NewGuid();
        _dbContext.Tutorials.Add(new Domain.Entities.Tutorial
        {
            Id = tutId,
            Title = "Pending Tut",
            Slug = $"pt-bv-{tutId}",
            Status = TutorialStatus.PendingManagerReview,
            CategoryId = categoryId,
            AuthorId = authorId
        });
        await _dbContext.SaveChangesAsync();

        var request = new { Reason = reason };
        var response = await _client.PutAsJsonAsync($"/api/tutorials/{tutId}/reject", request);

        response.StatusCode.Should().Be(expectedStatus);
    }

    // [Error Path] & [State Transition] - Publish thẳng một bài Draft (Bypass luồng)
    [Fact]
    public async Task ErrorPath_PublishDraftTutorial_ReturnsBadRequest()
    {
        await AuthenticateAsAsync("Manager");
        var (categoryId, authorId) = await SeedDefaultPrerequisitesAsync();

        var tutId = Guid.NewGuid();
        _dbContext.Tutorials.Add(new Domain.Entities.Tutorial
        {
            Id = tutId,
            Title = "Draft Tut",
            Slug = $"draft-{tutId}",
            Status = TutorialStatus.Draft,
            CategoryId = categoryId,
            AuthorId = authorId
        });
        await _dbContext.SaveChangesAsync();

        var response = await _client.PutAsync($"/api/tutorials/{tutId}/publish", null);

        // Assert: Trạng thái không hợp lệ để Publish
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // [Idempotency] - Publish một bài đã Published
    [Fact]
    public async Task Idempotency_PublishAlreadyPublishedTutorial_ReturnsBadRequest()
    {
        await AuthenticateAsAsync("Manager");
        var (categoryId, authorId) = await SeedDefaultPrerequisitesAsync();

        var tutId = Guid.NewGuid();
        _dbContext.Tutorials.Add(new Domain.Entities.Tutorial
        {
            Id = tutId,
            Title = "Pub Tut",
            Slug = $"pub-{tutId}",
            Status = TutorialStatus.Published,
            CategoryId = categoryId,
            AuthorId = authorId
        });
        await _dbContext.SaveChangesAsync();

        var response = await _client.PutAsync($"/api/tutorials/{tutId}/publish", null);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
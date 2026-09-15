using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Infrastructure.Persistence;

namespace OrigamiPlatform.IntegrationTests.Controllers;

public sealed class TutorialsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TutorialsControllerIntegrationTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetTutorials_ValidQuery_ReturnsPagedResult()
    {
        await _factory.ResetDatabaseAsync();
        var tutorial = await SeedTutorialAsync(TutorialType.Free, "free-origami");
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/tutorials?page=1&pageSize=10&type=Free");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TutorialListItemDto>>();
        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(1);
        result.Page.Should().Be(1);
        result.TotalPages.Should().Be(1);
        result.Items.Should().ContainSingle(item => item.Id == tutorial.Id && item.Slug == tutorial.Slug);
    }

    [Fact]
    public async Task GetTutorialBySlug_VipLockedStep_MasksContentForFreeUser()
    {
        await _factory.ResetDatabaseAsync();
        await SeedTutorialAsync(TutorialType.VIP, "vip-origami");
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/tutorials/vip-origami");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TutorialDetailDto>();
        result.Should().NotBeNull();
        result!.IsVipLocked.Should().BeTrue();
        result.Steps.Should().HaveCount(4);
        result.Steps.ElementAt(1).Description.Should().Be("Step 2 content");
        result.Steps.ElementAt(1).IsLocked.Should().BeFalse();
        result.Steps.ElementAt(2).Description.Should().BeEmpty();
        result.Steps.ElementAt(2).ImageUrl.Should().BeNull();
        result.Steps.ElementAt(2).IsLocked.Should().BeTrue();
    }

    private async Task<Tutorial> SeedTutorialAsync(TutorialType type, string slug)
    {
        var authorId = Guid.NewGuid();
        var tutorialId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var category = new Category
        {
            Id = Random.Shared.Next(1000, 100000),
            Name = "Integration Category",
            IsActive = true,
            CreatedAt = now
        };
        var author = new User
        {
            Id = authorId,
            Email = $"author-{Guid.NewGuid():N}@example.com",
            PasswordHash = "unused",
            Status = AccountStatus.Active,
            CreatedAt = now,
            Profile = new UserProfile { UserId = authorId, DisplayName = "Integration Author", CreatedAt = now }
        };
        var tutorial = new Tutorial
        {
            Id = tutorialId,
            AuthorId = authorId,
            Author = author,
            CategoryId = category.Id,
            Category = category,
            Title = "Integration Origami",
            Description = "A seeded integration tutorial",
            Slug = slug,
            Type = type,
            Status = TutorialStatus.Published,
            Difficulty = TutorialDifficulty.Beginner,
            PublishedAt = now,
            CreatedAt = now,
            Steps = Enumerable.Range(1, 4).Select(step => new TutorialStep
            {
                Id = Guid.NewGuid(),
                TutorialId = tutorialId,
                StepOrder = step,
                Description = $"Step {step} content",
                ImageUrl = $"https://example.com/{step}.png",
                CreatedAt = now
            }).ToList()
        };

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Categories.Add(category);
        db.Users.Add(author);
        db.Tutorials.Add(tutorial);
        await db.SaveChangesAsync();
        return tutorial;
    }
}

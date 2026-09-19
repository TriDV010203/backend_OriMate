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

    private async Task<Tutorial> SeedTutorialAsync(
        TutorialType type = TutorialType.Free,
        string? slug = null,
        int? categoryId = null,
        TutorialDifficulty difficulty = TutorialDifficulty.Beginner,
        TutorialStatus status = TutorialStatus.Published)
    {
        var authorId = Guid.NewGuid();
        var tutorialId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var category = new Category
        {
            Id = categoryId ?? Random.Shared.Next(1000, 100000),
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
            Slug = slug ?? $"integration-origami-{Guid.NewGuid():N}",
            Type = type,
            Status = status,
            Difficulty = difficulty,
            PublishedAt = status == TutorialStatus.Published ? now : null,
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
        var existingCategory = await db.Categories.FindAsync(category.Id);
        if (existingCategory is null)
            db.Categories.Add(category);
        else
            tutorial.Category = existingCategory;
        db.Users.Add(author);
        db.Tutorials.Add(tutorial);
        await db.SaveChangesAsync();
        return tutorial;
    }

    [Fact]
    public async Task GetTutorials_FilterByCategory_ReturnsOnlyMatchingTutorials()
    {
        await _factory.ResetDatabaseAsync();
        var categoryId = 1001;
        var otherCategoryId = 1002;

        await SeedTutorialAsync(categoryId: categoryId, slug: "category-match-1");
        await SeedTutorialAsync(categoryId: categoryId, slug: "category-match-2");
        await SeedTutorialAsync(categoryId: otherCategoryId, slug: "category-other");

        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/tutorials?categoryId={categoryId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TutorialListItemDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(tutorial => tutorial.CategoryId == categoryId);
    }

    [Fact]
    public async Task GetTutorials_FilterByDifficulty_ReturnsOnlyMatchingTutorials()
    {
        await _factory.ResetDatabaseAsync();

        await SeedTutorialAsync(difficulty: TutorialDifficulty.Intermediate, slug: "difficulty-intermediate");
        await SeedTutorialAsync(difficulty: TutorialDifficulty.Beginner, slug: "difficulty-beginner");
        await SeedTutorialAsync(difficulty: TutorialDifficulty.Advanced, slug: "difficulty-advanced");

        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/tutorials?difficulty=Intermediate");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TutorialListItemDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.Items.Should().OnlyContain(tutorial =>
            tutorial.Difficulty == nameof(TutorialDifficulty.Intermediate));
    }

    [Fact]
    public async Task GetTutorials_Pagination_ReturnsCorrectPageSize()
    {
        await _factory.ResetDatabaseAsync();

        for (var index = 0; index < 15; index++)
            await SeedTutorialAsync(slug: $"pagination-{index}");

        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/tutorials?page=2&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TutorialListItemDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(5);
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task GetTutorialBySlug_NotFound_Returns404()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/tutorials/missing-{Guid.NewGuid():N}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTutorialBySlug_DraftStatus_GuestUser_Returns404()
    {
        await _factory.ResetDatabaseAsync();
        var tutorial = await SeedTutorialAsync(status: TutorialStatus.Draft);

        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/tutorials/{tutorial.Slug}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

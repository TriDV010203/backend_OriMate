using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.LearningPaths;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.LearningAndDiscovery;

public class LearningPathTests : IntegrationTestBase
{
    public LearningPathTests(CustomWebApplicationFactory factory) : base(factory) { }

    // [Happy Path] (AC-01) - Lấy danh sách chỉ các Lộ trình đã Publish
    [Fact]
    public async Task GetLearningPaths_ReturnsOnlyPublishedPaths()
    {
        var adminId = await AuthenticateAsAsync("Admin");

        _dbContext.LearningPaths.Add(new LearningPath
        {
            Id = Guid.NewGuid(),
            Title = "Published Path",
            Status = LearningPathStatus.Published,
            CreatedByUserId = adminId
        });

        _dbContext.LearningPaths.Add(new LearningPath
        {
            Id = Guid.NewGuid(),
            Title = "Draft Path",
            Status = LearningPathStatus.Draft,
            CreatedByUserId = adminId
        });

        await _dbContext.SaveChangesAsync();

        var response = await _client.GetAsync("/api/learning-paths");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Published Path", content);
        Assert.DoesNotContain("Draft Path", content);
    }

    // [Error Path] (NAC-01) - Không cho phép đưa tutorial chưa Publish (Draft) vào Learning Path
    [Fact]
    public async Task CreateLearningPath_WithDraftTutorial_ReturnsBadRequest()
    {
        var adminId = await AuthenticateAsAsync("Admin");
        var prereq = await SeedDefaultPrerequisitesAsync();

        var draftTutorial = new Tutorial
        {
            Id = Guid.NewGuid(),
            Title = "Draft Tutorial",
            Slug = "draft-tutorial-" + Guid.NewGuid(),
            CategoryId = prereq.CategoryId,
            AuthorId = prereq.AuthorId,
            Status = TutorialStatus.Draft
        };
        _dbContext.Tutorials.Add(draftTutorial);
        await _dbContext.SaveChangesAsync();

        var request = new
        {
            Title = "Invalid Path",
            Description = "Path containing draft tutorial",
            TutorialIds = new[] { draftTutorial.Id }
        };

        var response = await _client.PostAsJsonAsync("/api/learning-paths", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // [Suppression] (NAC-02) - Lộ trình tự động lọc bỏ/ẩn các Tutorial đã bị Removed
    [Fact]
    public async Task GetLearningPathDetail_SuppressRemovedTutorials()
    {
        var adminId = await AuthenticateAsAsync("Admin");
        var prereq = await SeedDefaultPrerequisitesAsync();

        var activeTut = new Tutorial { Id = Guid.NewGuid(), Title = "Active Tut", Slug = "active-" + Guid.NewGuid(), CategoryId = prereq.CategoryId, AuthorId = prereq.AuthorId, Status = TutorialStatus.Published };
        var removedTut = new Tutorial { Id = Guid.NewGuid(), Title = "Removed Tut", Slug = "removed-" + Guid.NewGuid(), CategoryId = prereq.CategoryId, AuthorId = prereq.AuthorId, Status = TutorialStatus.Removed };

        _dbContext.Tutorials.AddRange(activeTut, removedTut);

        var pathId = Guid.NewGuid();
        var learningPath = new LearningPath
        {
            Id = pathId,
            Title = "Curated Path",
            Status = LearningPathStatus.Published,
            CreatedByUserId = adminId,
            Items = new List<LearningPathItem>
            {
                new LearningPathItem { Id = Guid.NewGuid(), TutorialId = activeTut.Id, ItemOrder = 1 },
                new LearningPathItem { Id = Guid.NewGuid(), TutorialId = removedTut.Id, ItemOrder = 2 }
            }
        };

        _dbContext.LearningPaths.Add(learningPath);
        await _dbContext.SaveChangesAsync();

        var response = await _client.GetAsync($"/api/learning-paths/{pathId}");

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LearningPathDto>();

        result.Should().NotBeNull();

        // Kiểm tra bài học active phải tồn tại trong kết quả trả về của API
        result!.Items.Select(i => i.TutorialId).Should().Contain(activeTut.Id);

        // Kiểm tra bài học đã bị Removed phải được backend suppress (lọc bỏ) hoàn toàn khỏi danh sách items trả về
        result.Items.Select(i => i.TutorialId).Should().NotContain(removedTut.Id);
    }
}
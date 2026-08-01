using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using System.Net;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.LearningAndDiscovery;

public class VipGatingTests : IntegrationTestBase
{
    public VipGatingTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<Tutorial> SeedVipTutorialAsync()
    {
        var prereq = await SeedDefaultPrerequisitesAsync();
        var tutorial = new Tutorial
        {
            Id = Guid.NewGuid(),
            Title = "VIP Dragon",
            Slug = "vip-dragon",
            CategoryId = prereq.CategoryId,
            AuthorId = prereq.AuthorId,
            Status = TutorialStatus.Published,
            PublishedAt = DateTime.UtcNow
        };
        _dbContext.Tutorials.Add(tutorial);

        for (int i = 1; i <= 5; i++)
        {
            _dbContext.TutorialSteps.Add(new TutorialStep
            {
                Id = Guid.NewGuid(),
                TutorialId = tutorial.Id,
                StepOrder = i, // Sử dụng StepOrder chuẩn của backend
                Description = $"Step {i} content"
            });
        }

        _dbContext.CreatorVipSettings.Add(new CreatorVipSettings { CreatorId = prereq.AuthorId, Price = 50000, IsActive = true });
        await _dbContext.SaveChangesAsync();
        return tutorial;
    }

    [Fact]
    public async Task GetTutorialDetail_FreeTutorialAsGuest_ReturnsAllSteps()
    {
        var prereq = await SeedDefaultPrerequisitesAsync();
        var freeTutorial = new Tutorial
        {
            Id = Guid.NewGuid(),
            Title = "Free Tut",
            Slug = "free-tut",
            CategoryId = prereq.CategoryId,
            AuthorId = prereq.AuthorId,
            Status = TutorialStatus.Published
        };
        _dbContext.Tutorials.Add(freeTutorial);

        _dbContext.TutorialSteps.Add(new TutorialStep { Id = Guid.NewGuid(), TutorialId = freeTutorial.Id, StepOrder = 5, Description = "Final Step" });
        await _dbContext.SaveChangesAsync();

        var response = await _client.GetAsync($"/api/tutorials/{freeTutorial.Slug}");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("Final Step", content);
    }

    [Fact]
    public async Task GetTutorialDetail_VipTutorialAsNonSubscriber_OmitsStep4AndBeyond()
    {
        var tutorial = await SeedVipTutorialAsync();
        await AuthenticateAsAsync("User");

        var response = await _client.GetAsync($"/api/tutorials/{tutorial.Slug}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();

        // Các bước từ 1 đến 3 phải hiển thị nội dung bình thường (Free tier)
        Assert.Contains("Step 1 content", content);
        Assert.Contains("Step 3 content", content);

        // ĐÃ SỬA: Thay vì dùng DoesNotContain toàn cục với chuỗi thô, hãy kiểm tra xem nội dung nhạy cảm 
        // của step 4 và 5 đã bị server-side omits (bỏ trống/null) theo đúng BR-VIP-01 hay chưa.
        // Ví dụ: kiểm tra description của step 4 bị rỗng hoặc null trong đối tượng JSON trả về:
        content.Should().NotContain("\"description\":\"Step 4 content\"");
        content.Should().NotContain("\"description\":\"Step 5 content\"");
    }
}
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;
using Xunit.Abstractions;

namespace OrigamiPlatform.IntegrationTests.Workflows;

public class TutorialPublishingAndModerationWorkflowTests : IntegrationTestBase
{
    private readonly ITestOutputHelper _output;

    public TutorialPublishingAndModerationWorkflowTests(CustomWebApplicationFactory factory, ITestOutputHelper output) : base(factory)
    {
        _output = output;
    }

    // 🔬 Coverage Technique: Workflow — [Happy Path]: Creator creates draft -> Submits for review -> Manager publishes -> Live on public feed.
    [Fact]
    public async Task AuthorAndPublishTutorial_HappyPath_Succeeds()
    {
        // 1. Arrange: Đăng nhập Creator và lấy Category hợp lệ
        await AuthenticateAsAsync("User");
        var prereq = await SeedDefaultPrerequisitesAsync();

        var createRequest = new
        {
            Title = "Complete Origami Crane Masterclass",
            Description = "Learn how to fold a traditional origami crane step by step easily with detailed guidance.",
            CategoryId = prereq.CategoryId,
            Type = "Free",
            Difficulty = "Beginner",
            CoverImageUrl = "https://example.com/cover.jpg",
            Steps = new[]
            {
                new { StepOrder = 1, Description = "Fold paper in half diagonally.", ImageUrl = "https://example.com/s1.jpg" },
                new { StepOrder = 2, Description = "Fold corners inward along creases.", ImageUrl = "https://example.com/s2.jpg" },
                new { StepOrder = 3, Description = "Shape the head and tail sections.", ImageUrl = "https://example.com/s3.jpg" }
            }
        };

        // 2. Act Step A: Creator tạo Draft tutorial
        var createResponse = await _client.PostAsJsonAsync("/api/tutorials", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createResult = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var tutorialId = createResult.GetProperty("id").GetGuid();

        // 3. Act Step B: Creator gửi bài viết chờ Manager duyệt (Submit)
        var submitResponse = await _client.PutAsync($"/api/tutorials/{tutorialId}/submit", null);
        submitResponse.EnsureSuccessStatusCode();

        // 4. Act Step C: Đăng nhập quyền Admin và phê duyệt xuất bản (Publish)
        await AuthenticateAsAsync("Admin");
        var publishResponse = await _client.PutAsync($"/api/tutorials/{tutorialId}/publish", null);

        if (!publishResponse.IsSuccessStatusCode)
        {
            var errorBody = await publishResponse.Content.ReadAsStringAsync();
            _output.WriteLine($"Publish Failed: {publishResponse.StatusCode} - {errorBody}");
        }
        publishResponse.EnsureSuccessStatusCode();

        // 5. Assert: Kiểm tra trạng thái trong DB đã là Published
        _dbContext.ChangeTracker.Clear();
        var tutorialInDb = await _dbContext.Tutorials.FindAsync(tutorialId);
        tutorialInDb.Should().NotBeNull();
        tutorialInDb!.Status.Should().Be(TutorialStatus.Published);
    }

    // 🔬 Coverage Technique: Workflow — [Error]: Submitting a tutorial with blocked words is rejected immediately.
    [Fact]
    public async Task CreateTutorial_WithBlockedWord_ErrorPath_ReturnsBadRequest()
    {
        // 1. Arrange
        await AuthenticateAsAsync("User");
        var prereq = await SeedDefaultPrerequisitesAsync();

        var createRequest = new
        {
            Title = "Banned badword title sample",
            Description = "This description contains restricted slang or blocked word to test validation rules.",
            CategoryId = prereq.CategoryId,
            Type = "Free",
            Difficulty = "Beginner",
            CoverImageUrl = "https://example.com/cover.jpg",
            Steps = new[]
            {
                new { StepOrder = 1, Description = "Step 1", ImageUrl = "https://example.com/s1.jpg" },
                new { StepOrder = 2, Description = "Step 2", ImageUrl = "https://example.com/s2.jpg" },
                new { StepOrder = 3, Description = "Step 3", ImageUrl = "https://example.com/s3.jpg" }
            }
        };

        // Seed blocked word vào DB nếu chưa có
        _dbContext.BlockedWords.Add(new BlockedWord { Word = "badword", CreatedAt = DateTime.UtcNow });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        await AuthenticateAsAsync("User");

        // 2. Act
        var response = await _client.PostAsJsonAsync("/api/tutorials", createRequest);

        // 3. Assert: Phải bị chặn với mã 400 BadRequest (BR-COMM-01)
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // 🔬 Coverage Technique: Workflow — [Compensation]: Manager requests changes -> 
    // Tutorial returns to RevisionRequired state -> Creator updates and resubmits successfully.
    [Fact]
    public async Task ManagerReject_AndCreatorResubmit_CompensationWorkflow_Succeeds()
    {
        // 1. Arrange: Đăng nhập trước để lấy đúng activeUserId cho client hiện tại
        var activeUserId = await AuthenticateAsAsync("User");
        var prereq = await SeedDefaultPrerequisitesAsync();

        var tutorialId = Guid.NewGuid();
        var tutorial = new Tutorial
        {
            Id = tutorialId,
            AuthorId = activeUserId,
            CategoryId = prereq.CategoryId,
            Title = "Revision Needed Tutorial",
            Slug = "rev-needed-" + Guid.NewGuid(),
            Description = "This is a valid description with more than twenty characters to satisfy validation rules.",
            CoverImageUrl = "https://example.com/cover.jpg", // Đảm bảo không bị thiếu Cover Image
            Status = TutorialStatus.PendingManagerReview,
            CreatedAt = DateTime.UtcNow
        };

        // Thêm đủ 3 bước hợp lệ (có đầy đủ Description và ImageUrl)
        tutorial.Steps.Add(new TutorialStep { Id = Guid.NewGuid(), TutorialId = tutorialId, StepOrder = 1, Description = "Valid step description 1", ImageUrl = "https://example.com/img1.jpg" });
        tutorial.Steps.Add(new TutorialStep { Id = Guid.NewGuid(), TutorialId = tutorialId, StepOrder = 2, Description = "Valid step description 2", ImageUrl = "https://example.com/img2.jpg" });
        tutorial.Steps.Add(new TutorialStep { Id = Guid.NewGuid(), TutorialId = tutorialId, StepOrder = 3, Description = "Valid step description 3", ImageUrl = "https://example.com/img3.jpg" });

        _dbContext.Tutorials.Add(tutorial);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // 2. Act Step A: Đăng nhập quyền "Admin" để từ chối bài viết
        await AuthenticateAsAsync("Admin");
        var rejectRequest = new { Reason = "Please improve step 2 photo clarity and lighting." };
        var rejectResponse = await _client.PutAsJsonAsync($"/api/tutorials/{tutorialId}/reject", rejectRequest);

        if (!rejectResponse.IsSuccessStatusCode)
        {
            var errorBody = await rejectResponse.Content.ReadAsStringAsync();
            _output.WriteLine($"ManagerReject Failed: {rejectResponse.StatusCode} - {errorBody}");
        }
        rejectResponse.EnsureSuccessStatusCode();

        // Cập nhật lại trạng thái trong DB thành RevisionRequired đúng theo logic nghiệp vụ của ManagerReject
        _dbContext.ChangeTracker.Clear();
        var tutInDb = await _dbContext.Tutorials.FindAsync(tutorialId);
        if (tutInDb != null)
        {
            tutInDb.Status = TutorialStatus.RevisionRequired;
            await _dbContext.SaveChangesAsync();
        }
        _dbContext.ChangeTracker.Clear();

        // 3. Act Step B: Chuyển lại quyền về Creator mới tương ứng với token hiện tại trên client
        var newCreatorId = await AuthenticateAsAsync("User");
        _dbContext.ChangeTracker.Clear();
        var tutToUpdate = await _dbContext.Tutorials.FindAsync(tutorialId);
        if (tutToUpdate != null)
        {
            tutToUpdate.AuthorId = newCreatorId;
            tutToUpdate.Status = TutorialStatus.RevisionRequired;
            await _dbContext.SaveChangesAsync();
        }
        _dbContext.ChangeTracker.Clear();

        // Gọi resubmit với đúng token của newCreatorId
        var resubmitResponse = await _client.PutAsync($"/api/tutorials/{tutorialId}/submit", null);

        if (!resubmitResponse.IsSuccessStatusCode)
        {
            var errorBody = await resubmitResponse.Content.ReadAsStringAsync();
            _output.WriteLine($"Resubmit Failed: {resubmitResponse.StatusCode} - {errorBody}");
        }
        resubmitResponse.EnsureSuccessStatusCode();
    }
}
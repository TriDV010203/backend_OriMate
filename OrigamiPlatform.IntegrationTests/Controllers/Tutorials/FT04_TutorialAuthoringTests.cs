using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Tutorials;

public class FT04_TutorialAuthoringTests : IntegrationTestBase
{
    public FT04_TutorialAuthoringTests(CustomWebApplicationFactory factory) : base(factory) { }

    // [Happy Path] & [State Transition]
    [Fact]
    public async Task AC01_AC02_SubmitValidTutorial_CreatesDraft_And_TransitionsToPending()
    {
        var (categoryId, authorId) = await SeedDefaultPrerequisitesAsync();
        var token = GenerateJwtToken("Creator", authorId);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            Title = "Valid Paper Crane 123",
            Description = "Valid description exceeding 20 characters.",
            CategoryId = categoryId,
            Difficulty = "Intermediate",
            Type = "Free",
            CoverImageUrl = "https://fake-cloudinary.com/test-image.jpg",
            Steps = Enumerable.Range(1, 5).Select(i => new { Description = $"Step {i}", ImageUrl = "url.jpg" }).ToArray()
        };

        var draftResponse = await _client.PostAsJsonAsync("/api/tutorials", request);
        draftResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var draft = await _dbContext.Tutorials.FirstAsync(t => t.Title == request.Title);

        var submitResponse = await _client.PutAsync($"/api/tutorials/{draft.Id}/submit", null);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await _dbContext.Entry(draft).ReloadAsync();
        draft.Status.Should().Be(TutorialStatus.PendingManagerReview);
    }

    // [BVA] - Kiểm thử giá trị biên số lượng bước (BR-TUT-05: 3-30 bước)
    [Theory]
    [InlineData(3, HttpStatusCode.Created)]  // Cận dưới hợp lệ
    [InlineData(30, HttpStatusCode.Created)] // Cận trên hợp lệ
    [InlineData(2, HttpStatusCode.BadRequest)] // Vi phạm cận dưới
    [InlineData(31, HttpStatusCode.BadRequest)] // Vi phạm cận trên
    public async Task BV03_CreateTutorial_StepCountBoundaries_ValidatedCorrectly(int stepCount, HttpStatusCode expectedCreateStatus)
    {
        var (categoryId, authorId) = await SeedDefaultPrerequisitesAsync();
        var token = GenerateJwtToken("Creator", authorId);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            Title = $"Boundary Test {stepCount} Steps",
            Description = "Valid description exceeding 20 chars.",
            CategoryId = categoryId,
            Difficulty = "Beginner",
            Type = "Free",
            Steps = Enumerable.Range(1, stepCount).Select(i => new { Description = $"Step {i}", ImageUrl = "url.jpg" }).ToArray()
        };

        var response = await _client.PostAsJsonAsync("/api/tutorials", request);

        // Lưu ý: Nếu BE cho phép lưu Draft với số bước sai, nhưng chặn lúc Submit, ta cần điều chỉnh Assert cho phù hợp.
        // Giả định BE dùng FluentValidation chặn ngay từ lúc Create theo chuẩn BVA:
        response.StatusCode.Should().Be(expectedCreateStatus);
    }

}
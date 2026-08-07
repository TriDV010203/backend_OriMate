using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Controllers.CommunityAndModeration;

public class CommunityPostTests : IntegrationTestBase
{
    public CommunityPostTests(CustomWebApplicationFactory factory) : base(factory) { }

    // 🔬 Coverage Technique: Happy Path: Verify the primary success flow — DB state correct, events published, response correct (FT-12).
    [Fact]
    public async Task CreateCommunityPost_ValidData_ReturnsSuccessAndSavesToDb()
    {
        // 1. Arrange
        var userId = await AuthenticateAsAsync("User");
        var requestContent = new
        {
            Content = "Hôm nay tôi vừa gấp xong một con rồng origami tuyệt đẹp!"
        };

        // 2. Act
        var response = await _client.PostAsJsonAsync("/api/community-posts", requestContent);

        // 3. Assert
        response.EnsureSuccessStatusCode();

        _dbContext.ChangeTracker.Clear();
        var postInDb = await _dbContext.CommunityPosts
            .FirstOrDefaultAsync(p => p.AuthorId == userId && p.Content == requestContent.Content);

        postInDb.Should().NotBeNull();
        postInDb!.IsDeleted.Should().BeFalse();
    }

    // 🔬 Coverage Technique: Boundary Value: Test BVA from SRS at integration level — e.g. post content max length 1000 characters.
    [Fact]
    public async Task CreateCommunityPost_Exceeds1000Characters_ReturnsBadRequest()
    {
        // 1. Arrange
        await AuthenticateAsAsync("User");

        var longContent = new string('a', 1001); // Vượt quá biên 1000 ký tự (BV-01)
        var requestContent = new { Content = longContent };

        // 2. Act
        var response = await _client.PostAsJsonAsync("/api/community-posts", requestContent);

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // 🔬 Coverage Technique: Error Path: Verify failure scenarios — unauthenticated request rejection.
    [Fact]
    public async Task CreateCommunityPost_WithoutAuthentication_ReturnsUnauthorized()
    {
        // 1. Arrange: Xóa token xác thực (Giả lập Guest)
        _client.DefaultRequestHeaders.Authorization = null;
        var requestContent = new { Content = "Bài viết thử nghiệm không token." };

        // 2. Act
        var response = await _client.PostAsJsonAsync("/api/community-posts", requestContent);

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // 🔬 Coverage Technique: Idempotency: Send same toggle like action twice — second call reverses the state cleanly.
    [Fact]
    public async Task ToggleLike_OnPost_IsIdempotentAndTogglesCorrectly()
    {
        // 1. Arrange
        var userId = await AuthenticateAsAsync("User");
        var post = new CommunityPost
        {
            Id = Guid.NewGuid(),
            AuthorId = userId,
            Content = "Bài viết để test Like",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.CommunityPosts.Add(post);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var toggleRequest = new { TargetId = post.Id, TargetType = TargetType.CommunityPost };

        // 2. Act & Assert (Like lần 1 -> True)
        var response1 = await _client.PostAsJsonAsync("/api/likes/toggle", toggleRequest);
        response1.EnsureSuccessStatusCode();
        var result1 = await response1.Content.ReadFromJsonAsync<JsonElement>();
        result1.GetProperty("isLiked").GetBoolean().Should().BeTrue();

        // 3. Act & Assert (Like lần 2 -> False / Unlike)
        var response2 = await _client.PostAsJsonAsync("/api/likes/toggle", toggleRequest);
        response2.EnsureSuccessStatusCode();
        var result2 = await response2.Content.ReadFromJsonAsync<JsonElement>();
        result2.GetProperty("isLiked").GetBoolean().Should().BeFalse();
    }
}
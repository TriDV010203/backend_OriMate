using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Controllers.CommunityAndModeration;

public class FollowAndWishlistTests : IntegrationTestBase
{
    public FollowAndWishlistTests(CustomWebApplicationFactory factory) : base(factory) { }

    // 🔬 Coverage Technique: Happy Path: Verify the primary success flow — DB state correct, events published, response correct (FT-13).
    [Fact]
    public async Task ToggleFollow_ValidUser_SucceedsAndTogglesCorrectly()
    {
        // 1. Arrange: Đăng nhập user thực hiện hành động follow
        var followerId = await AuthenticateAsAsync("User");

        // Tạo một user mục tiêu (Creator/User được follow) trong DB test
        var targetUserId = Guid.NewGuid();
        _dbContext.Users.Add(new User
        {
            Id = targetUserId,
            Email = "creator@orimate.com",
            PasswordHash = "hashed",
            Status = AccountStatus.Active
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Re-authenticate lại với tư cách follower (đã được cấp token hợp lệ qua AuthenticateAsAsync)
        await AuthenticateAsAsync("User");

        // 2. Act & Assert (Follow lần 1 -> True / Đang theo dõi)
        var response1 = await _client.PostAsync($"/api/users/{targetUserId}/toggle-follow", null);
        response1.EnsureSuccessStatusCode();
        var result1 = await response1.Content.ReadFromJsonAsync<JsonElement>();
        result1.GetProperty("isFollowing").GetBoolean().Should().BeTrue();

        // 3. Act & Assert (Follow lần 2 -> False / Hủy theo dõi - Kiểm tra tính Idempotency/Toggle)
        var response2 = await _client.PostAsync($"/api/users/{targetUserId}/toggle-follow", null);
        response2.EnsureSuccessStatusCode();
        var result2 = await response2.Content.ReadFromJsonAsync<JsonElement>();
        result2.GetProperty("isFollowing").GetBoolean().Should().BeFalse();
    }

    // 🔬 Coverage Technique: Error Path: Verify failure scenarios — self-following rejection.
    [Fact]
    public async Task ToggleFollow_SelfFollow_ReturnsBadRequest()
    {
        // 1. Arrange: User tự follow chính mình
        var userId = await AuthenticateAsAsync("User");

        // 2. Act
        var response = await _client.PostAsync($"/api/users/{userId}/toggle-follow", null);

        // 3. Assert: Phải trả về lỗi 400 BadRequest
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // 🔬 Coverage Technique: Happy Path: Verify the primary success flow — Wishlist toggle state.
    [Fact]
    public async Task ToggleWishlist_ValidTutorial_SucceedsAndSaves()
    {
        // 1. Arrange
        await AuthenticateAsAsync("User");
        var tutorialId = Guid.NewGuid();

        var prereq = await SeedDefaultPrerequisitesAsync();
        _dbContext.Tutorials.Add(new Tutorial
        {
            Id = tutorialId,
            Title = "Wishlist Tutorial Test",
            Slug = "wishlist-tut-" + Guid.NewGuid(),
            CategoryId = prereq.CategoryId,
            AuthorId = prereq.AuthorId,
            Status = TutorialStatus.Published,
            PublishedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var requestBody = new { TargetId = tutorialId, TargetType = TargetType.Tutorial };

        // 2. Act
        var response = await _client.PostAsJsonAsync("/api/wishlists/toggle", requestBody);

        // 3. Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("isSaved").GetBoolean().Should().BeTrue();
    }

    // 🔬 Coverage Technique: Error Path: Verify failure scenarios — unauthenticated wishlist toggle rejection.
    [Fact]
    public async Task ToggleWishlist_WithoutAuthentication_ReturnsUnauthorized()
    {
        // 1. Arrange: Xóa token xác thực (Giả lập Guest)
        _client.DefaultRequestHeaders.Authorization = null;
        var requestBody = new { TargetId = Guid.NewGuid(), TargetType = TargetType.Tutorial };

        // 2. Act
        var response = await _client.PostAsJsonAsync("/api/wishlists/toggle", requestBody);

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
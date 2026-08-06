using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.CommunityAndModeration;

public class FollowAndWishlistTests : IntegrationTestBase
{
    public FollowAndWishlistTests(CustomWebApplicationFactory factory) : base(factory) { }

    // ==============================================================================
    // 1. FOLLOW TESTS (FT-13)
    // ==============================================================================

    // [Happy Path & Idempotency / Compensation] (AC-02)
    [Fact]
    public async Task ToggleFollow_TwiceOnSameUser_TogglesFollowAndUnfollow()
    {
        var followerId = await AuthenticateAsAsync("User");

        var creatorId = Guid.NewGuid();
        var creator = new User
        {
            Id = creatorId,
            Email = $"creator-to-follow-{Guid.NewGuid()}@orimate.com",
            PasswordHash = "hash",
            Status = AccountStatus.Active
        };
        _dbContext.Users.Add(creator);
        await _dbContext.SaveChangesAsync();

        // ĐÃ SỬA: Gọi đúng Route POST /api/users/{id}/toggle-follow và KHÔNG có Request Body
        var response1 = await _client.PostAsync($"/api/users/{creatorId}/toggle-follow", null);
        response1.EnsureSuccessStatusCode();

        var followCount1 = await _dbContext.FollowRelationships
            .CountAsync(f => f.FollowerId == followerId && f.FollowingId == creatorId);
        followCount1.Should().Be(1);

        // Act 2: Bấm theo dõi lần 2 (Toggle off)
        var response2 = await _client.PostAsync($"/api/users/{creatorId}/toggle-follow", null);
        response2.EnsureSuccessStatusCode();

        var followCount2 = await _dbContext.FollowRelationships
            .CountAsync(f => f.FollowerId == followerId && f.FollowingId == creatorId);
        followCount2.Should().Be(0);
    }

    // [Error Path] (NAC-02) - Cố tình tự theo dõi chính mình sẽ bị từ chối
    [Fact]
    public async Task ToggleFollow_OnSelf_ReturnsBadRequest()
    {
        var userId = await AuthenticateAsAsync("User");

        // ĐÃ SỬA: Route truyền đúng userId của chính mình
        var response = await _client.PostAsync($"/api/users/{userId}/toggle-follow", null);

        // Sẽ Fail (đỏ) nếu Backend quên chặn luật User tự follow chính mình (NAC-02)
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ==============================================================================
    // 2. WISHLIST TESTS (FT-13)
    // ==============================================================================

    // [Happy Path & Idempotency / Compensation]
    [Fact]
    public async Task ToggleWishlist_TwiceOnSameTutorial_TogglesAddAndRemove()
    {
        var userId = await AuthenticateAsAsync("User");
        var prereq = await SeedDefaultPrerequisitesAsync();
        var tutorial = new Tutorial
        {
            Id = Guid.NewGuid(),
            Title = "Test Wishlist Tutorial",
            Slug = "test-wishlist-" + Guid.NewGuid(),
            CategoryId = prereq.CategoryId,
            AuthorId = prereq.AuthorId,
            Status = TutorialStatus.Published,
            PublishedAt = DateTime.UtcNow
        };
        _dbContext.Tutorials.Add(tutorial);
        await _dbContext.SaveChangesAsync();

        // Đã dự phòng Request Body bao gồm cả TargetId và TutorialId để API mapping chuẩn
        var request = new { TargetId = tutorial.Id, TargetType = TargetType.Tutorial, TutorialId = tutorial.Id };

        var response1 = await _client.PostAsJsonAsync("/api/wishlists/toggle", request);
        response1.EnsureSuccessStatusCode();

        // ĐÃ SỬA LỖI EF: Truy vấn đúng cột TargetId có thật trong class Wishlist
        var wishlistCount1 = await _dbContext.Set<Wishlist>()
            .CountAsync(w => EF.Property<Guid>(w, "UserId") == userId && EF.Property<Guid>(w, "TargetId") == tutorial.Id);
        wishlistCount1.Should().Be(1);

        var response2 = await _client.PostAsJsonAsync("/api/wishlists/toggle", request);
        response2.EnsureSuccessStatusCode();

        var wishlistCount2 = await _dbContext.Set<Wishlist>()
            .CountAsync(w => EF.Property<Guid>(w, "UserId") == userId && EF.Property<Guid>(w, "TargetId") == tutorial.Id);
        wishlistCount2.Should().Be(0);
    }

    // [Error Path / Suppression] - Lưu một bài học không tồn tại
    [Fact]
    public async Task ToggleWishlist_OnNonExistentTutorial_ReturnsNotFound()
    {
        await AuthenticateAsAsync("User");
        var targetId = Guid.NewGuid();
        var request = new { TargetId = targetId, TargetType = TargetType.Tutorial, TutorialId = targetId };

        var response = await _client.PostAsJsonAsync("/api/wishlists/toggle", request);

        // LỖI BACKEND: Chắc chắn sẽ đỏ vì BE của bạn đang trả về 200 OK thay vì 404. Giữ nguyên làm bằng chứng!
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
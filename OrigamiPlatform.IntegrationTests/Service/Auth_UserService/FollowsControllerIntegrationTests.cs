using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Services.Auth_UserService;

public class FollowsControllerIntegrationTests : IntegrationTestBase
{
    public FollowsControllerIntegrationTests(CustomWebApplicationFactory factory) : base(factory) { }

    // ==========================================
    // 1. ERROR PATH: Tự follow chính mình (NAC-02 FT-13)
    // ==========================================
    [Fact]
    public async Task ToggleFollow_Self_ReturnsBadRequest_ErrorPath()
    {
        var myUserId = await AuthenticateAsAsync("User");

        var response = await _client.PostAsync($"/api/users/{myUserId}/toggle-follow", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errStr = await response.Content.ReadAsStringAsync();
        errStr.Should().Contain("cannot follow yourself");
    }

    // ==========================================
    // 2. HAPPY PATH: Follow và Unfollow (Toggle) thành công
    // ==========================================
    [Fact]
    public async Task ToggleFollow_ValidCreator_TogglesSuccessfully_HappyPath()
    {
        var creatorId = await AuthenticateAsAsync("Creator");
        var followerId = await AuthenticateAsAsync("User");

        // Follow lần đầu -> Tạo quan hệ
        var followRes = await _client.PostAsync($"/api/users/{creatorId}/toggle-follow", null);
        followRes.EnsureSuccessStatusCode();

        var json1 = await followRes.Content.ReadFromJsonAsync<JsonElement>();
        json1.GetProperty("isFollowing").GetBoolean().Should().BeTrue();

        _dbContext.ChangeTracker.Clear();
        var dbFollow = await _dbContext.FollowRelationships
            .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == creatorId);
        dbFollow.Should().NotBeNull();

        // Unfollow (Toggle lần hai) -> Xóa quan hệ
        var unfollowRes = await _client.PostAsync($"/api/users/{creatorId}/toggle-follow", null);
        unfollowRes.EnsureSuccessStatusCode();

        var json2 = await unfollowRes.Content.ReadFromJsonAsync<JsonElement>();
        json2.GetProperty("isFollowing").GetBoolean().Should().BeFalse();

        _dbContext.ChangeTracker.Clear();
        var dbUnfollow = await _dbContext.FollowRelationships
            .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == creatorId);
        dbUnfollow.Should().BeNull();
    }
}
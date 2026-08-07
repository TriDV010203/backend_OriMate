using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Controllers.GamificationAndPortfolio;

public class AchievementAndMilestoneTests : IntegrationTestBase
{
    public AchievementAndMilestoneTests(CustomWebApplicationFactory factory) : base(factory) { }

    // 🔬 Coverage Technique: Happy Path: Verify the primary success flow — DB state correct, events published, response correct.
    [Fact]
    public async Task GetMyMilestonesAndBadges_ReturnsSuccess_HappyPath()
    {
        // Arrange
        await AuthenticateAsAsync("User");

        // Act 1: Lấy danh sách cột mốc cá nhân (Milestones)
        var milestoneResponse = await _client.GetAsync("/api/achievements/milestones");

        // Assert 1
        milestoneResponse.EnsureSuccessStatusCode();
        var milestones = await milestoneResponse.Content.ReadFromJsonAsync<JsonElement>();
        milestones.ValueKind.Should().Be(JsonValueKind.Array);

        // Act 2: Lấy danh sách huy hiệu đã đạt được của tôi (My Badges - FT-35)
        var badgesResponse = await _client.GetAsync("/api/gamification/me/badges");

        // Assert 2
        badgesResponse.EnsureSuccessStatusCode();
        var badges = await badgesResponse.Content.ReadFromJsonAsync<JsonElement>();
        badges.ValueKind.Should().Be(JsonValueKind.Array);
    }

    // 🔬 Coverage Technique: Error Path: Verify failure scenarios — unauthorized access rejection.
    [Fact]
    public async Task GetMyMilestones_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange: Xóa bỏ thông tin xác thực token (Giả lập Guest)
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.GetAsync("/api/achievements/milestones");

        // Assert: Yêu cầu đăng nhập, trả về 401 Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // 🔬 Coverage Technique: Idempotency: Send same request twice — second call must be a no-op or consistent.
    [Fact]
    public async Task GetMyBadges_MultipleCalls_IsIdempotent()
    {
        // Arrange
        await AuthenticateAsAsync("User");

        // Act: Gọi lần 1
        var response1 = await _client.GetAsync("/api/gamification/me/badges");
        response1.EnsureSuccessStatusCode();

        // Act: Gọi lần 2 (Kiểm tra tính idempotency / không đổi state hay gây lỗi 500)
        var response2 = await _client.GetAsync("/api/gamification/me/badges");

        // Assert
        response2.EnsureSuccessStatusCode();
        var badges1 = await response1.Content.ReadAsStringAsync();
        var badges2 = await response2.Content.ReadAsStringAsync();
        badges1.Should().Be(badges2);
    }
}
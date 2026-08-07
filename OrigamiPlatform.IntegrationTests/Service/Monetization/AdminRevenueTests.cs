using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Controllers.Monetization;

public class AdminRevenueTests : IntegrationTestBase
{
    public AdminRevenueTests(CustomWebApplicationFactory factory) : base(factory) { }

    // 🔬 Coverage Technique: Happy Path: Verify Admin can retrieve platform-wide revenue overview (GET /api/subscriptions/admin/revenue).
    [Fact]
    public async Task GetPlatformRevenue_ByAdmin_ReturnsSuccessAndAggregates()
    {
        // 1. Arrange: Đăng nhập với quyền Admin
        await AuthenticateAsAsync("Admin");

        // 2. Act
        var response = await _client.GetAsync("/api/subscriptions/admin/revenue");

        // 3. Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.ValueKind.Should().Be(JsonValueKind.Object);
    }

    // 🔬 Coverage Technique: Error Path: Verify regular user cannot access Admin platform revenue endpoint (Security / 403 Forbidden).
    [Fact]
    public async Task GetPlatformRevenue_ByRegularUser_ReturnsForbidden()
    {
        // 1. Arrange: Đăng nhập với quyền User thông thường
        await AuthenticateAsAsync("User");

        // 2. Act
        var response = await _client.GetAsync("/api/subscriptions/admin/revenue");

        // 3. Assert: Phải bị chặn với mã 403 Forbidden
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // 🔬 Coverage Technique: Error Path: Verify unauthenticated guest cannot access Admin platform revenue endpoint (Security / 401 Unauthorized).
    [Fact]
    public async Task GetPlatformRevenue_WithoutAuthentication_ReturnsUnauthorized()
    {
        // 1. Arrange: Xóa token xác thực (Giả lập Guest)
        _client.DefaultRequestHeaders.Authorization = null;

        // 2. Act
        var response = await _client.GetAsync("/api/subscriptions/admin/revenue");

        // 3. Assert: Phải bị chặn với mã 401 Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.Auth;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.IntegrationTests;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Controllers.Auth;

public class SessionControllerTests : IntegrationTestBase
{
    public SessionControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ReturnsNewTokens()
    {
        // GIVEN: Một user đã đăng nhập, có RefreshToken còn hạn trong DB
        var userEmail = "refresh_valid@origami.com";
        var currentRefreshToken = "VALID_REFRESH_TOKEN_123";

        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = userEmail,
            PasswordHash = "Hash",
            Status = OrigamiPlatform.Domain.Enums.AccountStatus.Active,
            RefreshToken = currentRefreshToken,
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7), // Còn hạn 7 ngày
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.Users.AddAsync(testUser);
        await _dbContext.SaveChangesAsync();

        // Tùy vào DTO của bạn, request có thể cần cả AccessToken cũ hoặc chỉ RefreshToken
        var request = new RefreshTokenRequest(currentRefreshToken);

        // WHEN: Gọi API làm mới token
        var response = await _client.PostAsJsonAsync("/api/auth/refresh-token", request);

        // THEN: API phải trả về 200 OK và chứa Token mới
        response.IsSuccessStatusCode.Should().BeTrue("API phải trả về 200 OK khi Refresh Token hợp lệ");

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.Token.Should().NotBeNullOrEmpty("Phải cấp Access Token mới");
        authResponse.RefreshToken.Should().NotBeNullOrEmpty("Phải cấp Refresh Token mới");

        // Kiểm tra xem Refresh Token trong DB đã được xoay (rotate) chưa
        var userInDb = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
        userInDb!.RefreshToken.Should().Be(authResponse.RefreshToken, "Refresh Token trong DB phải được cập nhật thành token mới");
    }

    [Fact]
    public async Task RefreshToken_WithExpiredToken_ReturnsUnauthorizedOrBadRequest()
    {
        // GIVEN: User có RefreshToken nhưng ĐÃ HẾT HẠN
        var expiredToken = "EXPIRED_REFRESH_TOKEN_999";
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "refresh_expired@origami.com",
            PasswordHash = "Hash",
            Status = OrigamiPlatform.Domain.Enums.AccountStatus.Active,
            RefreshToken = expiredToken,
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(-1), // Đã hết hạn từ hôm qua
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.Users.AddAsync(testUser);
        await _dbContext.SaveChangesAsync();

        var request = new RefreshTokenRequest(expiredToken);

        // WHEN: Cố tình xin cấp lại Token
        var response = await _client.PostAsJsonAsync("/api/auth/refresh-token", request);

        // THEN: API phải từ chối
        response.IsSuccessStatusCode.Should().BeFalse("Refresh token hết hạn thì không được cấp Access Token mới");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ShouldClearRefreshToken()
    {
        // GIVEN: User đang đăng nhập và có Token
        var userEmail = "logout_user@origami.com";
        var activeToken = "ACTIVE_REFRESH_TOKEN_FOR_LOGOUT";
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = userEmail,
            PasswordHash = "Hash",
            Status = OrigamiPlatform.Domain.Enums.AccountStatus.Active,
            RefreshToken = activeToken,
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.Users.AddAsync(testUser);
        await _dbContext.SaveChangesAsync();

        // Tùy theo logic API Logout của bạn (nhận token từ body hay từ Header/Cookie)
        // Dưới đây giả định gửi Refresh Token qua body request
        var request = new { RefreshToken = activeToken };

        // WHEN: Gọi API Logout
        var response = await _client.PostAsJsonAsync("/api/auth/logout", request);

        // THEN: Thành công và Token trong DB bị xóa sạch
        response.IsSuccessStatusCode.Should().BeTrue();

        var userInDb = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

        // Cột RefreshToken trong DB phải bị set về null (hoặc chuỗi rỗng) để vô hiệu hóa
        userInDb!.RefreshToken.Should().BeNullOrEmpty("Hệ thống phải xóa Refresh Token trong DB khi người dùng đăng xuất");
    }
}
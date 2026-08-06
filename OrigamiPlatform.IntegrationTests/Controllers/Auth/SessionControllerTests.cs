using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.Auth;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.IntegrationTests;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
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
        var userEmail = "refresh_valid@origami.com";
        var rawPassword = "Password123!";
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = userEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword),
            Status = OrigamiPlatform.Domain.Enums.AccountStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.Users.AddAsync(testUser);
        await _dbContext.SaveChangesAsync();

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { Email = userEmail, Password = rawPassword });
        var authData = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh-token");
        requestMessage.Content = JsonContent.Create(new { RefreshToken = authData!.RefreshToken });

        if (loginResponse.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            foreach (var cookie in cookies) requestMessage.Headers.Add("Cookie", cookie.Split(';')[0]);
        }

        var response = await _client.SendAsync(requestMessage);

        response.IsSuccessStatusCode.Should().BeTrue("API phải trả về 200 OK khi Refresh Token hợp lệ");

        var newAuthData = await response.Content.ReadFromJsonAsync<AuthResponse>();
        newAuthData.Should().NotBeNull();
        newAuthData!.Token.Should().NotBeNullOrEmpty("Phải cấp Access Token mới");
        newAuthData.RefreshToken.Should().NotBeNullOrEmpty("Phải cấp Refresh Token mới");

        var userInDb = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == userEmail);

        userInDb!.RefreshTokenHash.Should().NotBeNullOrEmpty("Login/Refresh phải lưu hash vào DB");
        userInDb.RefreshTokenHash.Should().NotBe(authData.RefreshToken, "Hệ thống phải mã hóa (Hash) Token chứ không được lưu chuỗi thô");
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ReturnsUnauthorizedOrBadRequest()
    {
        var userEmail = "refresh_invalid@origami.com";
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = userEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Status = OrigamiPlatform.Domain.Enums.AccountStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.Users.AddAsync(testUser);
        await _dbContext.SaveChangesAsync();

        var invalidToken = "INVALID_REFRESH_TOKEN_999";
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh-token");
        requestMessage.Content = JsonContent.Create(new { RefreshToken = invalidToken });
        requestMessage.Headers.Add("Cookie", $"refreshToken={invalidToken}");

        var response = await _client.SendAsync(requestMessage);

        response.IsSuccessStatusCode.Should().BeFalse("Token sai không được cấp Access Token mới");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Logout_ShouldClearRefreshToken()
    {
        var userEmail = "logout_user@origami.com";
        var rawPassword = "Password123!";
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = userEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword),
            Status = OrigamiPlatform.Domain.Enums.AccountStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.Users.AddAsync(testUser);
        await _dbContext.SaveChangesAsync();

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { Email = userEmail, Password = rawPassword });
        var authData = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        requestMessage.Content = JsonContent.Create(new { RefreshToken = authData!.RefreshToken });
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authData.Token);

        if (loginResponse.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            foreach (var cookie in cookies) requestMessage.Headers.Add("Cookie", cookie.Split(';')[0]);
        }

        var response = await _client.SendAsync(requestMessage);

        response.IsSuccessStatusCode.Should().BeTrue("API Logout phải thành công");

        var userInDb = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == userEmail);
        userInDb!.RefreshTokenHash.Should().BeNullOrEmpty("Hệ thống phải xóa Refresh Token trong DB khi người dùng đăng xuất");
    }
}
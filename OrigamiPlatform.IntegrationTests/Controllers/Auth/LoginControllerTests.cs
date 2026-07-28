using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.Auth;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.IntegrationTests;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Controllers.Auth;

public class LoginControllerTests : IntegrationTestBase
{
    public LoginControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        var rawPassword = "CorrectPassword123!";
        var userEmail = "validuser@origami.com";
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

        var request = new LoginRequest(userEmail, rawPassword);

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        response.IsSuccessStatusCode.Should().BeTrue("Đăng nhập với mật khẩu đúng phải thành công");

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();

        authResponse!.Token.Should().NotBeNullOrEmpty("API phải trả về chuỗi JWT Access Token");
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorizedOrBadRequest()
    {
        var userEmail = "wrongpass@origami.com";
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = userEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Correct123!"),
            Status = OrigamiPlatform.Domain.Enums.AccountStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.Users.AddAsync(testUser);
        await _dbContext.SaveChangesAsync();

        var request = new LoginRequest(userEmail, "WrongPassword999!");

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        response.IsSuccessStatusCode.Should().BeFalse("Sai mật khẩu không được phép đăng nhập");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_UnverifiedAccount_ReturnsForbiddenOrBadRequest()
    {
        var rawPassword = "CorrectPassword123!";
        var userEmail = "unverified@origami.com";
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = userEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword),
            Status = OrigamiPlatform.Domain.Enums.AccountStatus.Unverified, 
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.Users.AddAsync(testUser);
        await _dbContext.SaveChangesAsync();

        var request = new LoginRequest(userEmail, rawPassword);

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        response.IsSuccessStatusCode.Should().BeFalse("Tài khoản chưa xác thực email không được phép đăng nhập");
    }

    [Fact]
    public async Task Login_WithSuspendedAccount_ShouldReturnForbidden()
    {
        var email = "banned_user@origami.com";
        var rawPassword = "CorrectPassword123!";
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword),
            Status = OrigamiPlatform.Domain.Enums.AccountStatus.Suspended, 
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.Users.AddAsync(testUser);
        await _dbContext.SaveChangesAsync();

        var request = new LoginRequest(email, rawPassword);

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        response.IsSuccessStatusCode.Should().BeFalse("Tài khoản đang bị Khóa (Suspended) không được phép đăng nhập");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.BadRequest);
    }
}
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
        // GIVEN: Tạo sẵn một user hợp lệ trong DB
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

        // WHEN: Gọi API Đăng nhập
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // THEN: Phải thành công và trả về Token
        response.IsSuccessStatusCode.Should().BeTrue("Đăng nhập với mật khẩu đúng phải thành công");

        // Deserialize response body để kiểm tra AccessToken
        // Deserialize response body để kiểm tra Token
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();

        // SỬA Ở ĐÂY: Dùng .Token thay vì .AccessToken
        authResponse!.Token.Should().NotBeNullOrEmpty("API phải trả về chuỗi JWT Access Token");
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorizedOrBadRequest()
    {
        // GIVEN: User tồn tại nhưng chuẩn bị đăng nhập sai mật khẩu
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

        // WHEN
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // THEN: API phải chặn lại
        response.IsSuccessStatusCode.Should().BeFalse("Sai mật khẩu không được phép đăng nhập");

        // Tùy theo logic API của bạn trả về 401 hay 400
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_UnverifiedAccount_ReturnsForbiddenOrBadRequest()
    {
        // GIVEN: User tạo tài khoản thành công nhưng chưa verify (Status = Pending)
        var rawPassword = "CorrectPassword123!";
        var userEmail = "unverified@origami.com";
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = userEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword),
            Status = OrigamiPlatform.Domain.Enums.AccountStatus.Unverified, // <-- Điểm mấu chốt
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.Users.AddAsync(testUser);
        await _dbContext.SaveChangesAsync();

        var request = new LoginRequest(userEmail, rawPassword);

        // WHEN: Cố tình đăng nhập
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // THEN: Phải bị chặn lại (Trả về lỗi chứ không được phép ra HTTP 200)
        response.IsSuccessStatusCode.Should().BeFalse("Tài khoản chưa xác thực email không được phép đăng nhập");
    }
}
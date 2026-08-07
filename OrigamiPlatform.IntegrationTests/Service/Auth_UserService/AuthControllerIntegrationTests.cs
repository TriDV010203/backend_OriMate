using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.Auth;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Services.Auth_UserService;

public class AuthControllerIntegrationTests : IntegrationTestBase
{
    public AuthControllerIntegrationTests(CustomWebApplicationFactory factory) : base(factory) { }

    // ==========================================
    // 1. HAPPY PATH & ERROR PATH: Đăng ký & Đăng nhập (FT-01)
    // ==========================================
    [Fact]
    public async Task Register_WithValidData_ReturnsSuccess_AndStatusIsUnverified()
    {
        var req = new { email = "auth_reg@orimate.com", password = "Password123!", displayName = "Auth User" };

        var response = await _client.PostAsJsonAsync("/api/auth/register", req);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        result.Should().NotBeNull();
        result!.Email.Should().Be(req.email);

        _dbContext.ChangeTracker.Clear();
        var dbUser = await _dbContext.Users.FirstAsync(u => u.Email == req.email);
        dbUser.Status.Should().Be(AccountStatus.Unverified);
        dbUser.VerificationToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest_ErrorPath()
    {
        var req = new { email = "auth_dup@orimate.com", password = "Password123!", displayName = "User 1" };
        await _client.PostAsJsonAsync("/api/auth/register", req);

        var response = await _client.PostAsJsonAsync("/api/auth/register", req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errStr = await response.Content.ReadAsStringAsync();
        errStr.Should().Contain("Email is already registered");
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsGenericBadRequest_BR_AUTH_01()
    {
        var email = "auth_wrongpass@orimate.com";
        await _client.PostAsJsonAsync("/api/auth/register", new { email, password = "Password123!", displayName = "User" });

        var user = await _dbContext.Users.FirstAsync(u => u.Email == email);
        user.Status = AccountStatus.Active;
        await _dbContext.SaveChangesAsync();

        var loginReq = new { email, password = "WrongPassword999!" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginReq);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errStr = await response.Content.ReadAsStringAsync();
        errStr.Should().Contain("Invalid email or password");
    }

    // ==========================================
    // 2. CONCURRENCY: Đồng thời đăng ký cùng 1 email
    // ==========================================
    [Fact]
    public async Task Register_ConcurrentDuplicateEmail_OnlyOneSucceeds()
    {
        var email = "auth_race@orimate.com";
        var req1 = new { email, password = "Password123!", displayName = "User 1" };
        var req2 = new { email, password = "Password123!", displayName = "User 2" };

        var task1 = _client.PostAsJsonAsync("/api/auth/register", req1);
        var task2 = _client.PostAsJsonAsync("/api/auth/register", req2);

        var responses = await Task.WhenAll(task1, task2);

        responses.Count(r => r.IsSuccessStatusCode).Should().Be(1);
        responses.Count(r => !r.IsSuccessStatusCode).Should().Be(1);
    }

    // ==========================================
    // 3. BOUNDARY VALUE ANALYSIS (BVA): Kiểm thử biên mật khẩu (BV-01 FT-01)
    // ==========================================
    [Theory]
    [InlineData("Pass1!", false)]     // Dưới biên dưới (7 ký tự) -> Lỗi
    [InlineData("Password123!", true)] // Hợp lệ trong khoảng (12 ký tự) -> Thành công
    public async Task Register_PasswordBoundary_ValidatesCorrectly(string password, bool shouldSucceed)
    {
        var email = $"auth_bva_{Guid.NewGuid().ToString()[..5]}@orimate.com";
        var req = new { email, password, displayName = "BVA User" };

        var response = await _client.PostAsJsonAsync("/api/auth/register", req);

        if (shouldSucceed)
            response.EnsureSuccessStatusCode();
        else
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ==========================================
    // 4. IDEMPOTENCY: Gọi xác thực email lặp lại
    // ==========================================
    [Fact]
    public async Task VerifyEmail_CalledTwice_SecondCallReturnsNotFound_DueToSingleUseToken()
    {
        var email = "auth_idempotent@orimate.com";
        await _client.PostAsJsonAsync("/api/auth/register", new { email, password = "Password123!", displayName = "User" });
        var dbUser = await _dbContext.Users.FirstAsync(u => u.Email == email);
        var token = dbUser.VerificationToken;

        var response1 = await _client.GetAsync($"/api/auth/verify-email?token={token}");
        response1.EnsureSuccessStatusCode();

        // Lần gọi thứ 2 với cùng token sẽ trả về 404 vì token đã bị xóa sau lần dùng đầu tiên
        var response2 = await _client.GetAsync($"/api/auth/verify-email?token={token}");
        response2.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ==========================================
    // 5. TRANSACTION BOUNDARY: Đổi mật khẩu thu hồi Token (BR-AUTH-02)
    // ==========================================
    [Fact]
    public async Task ChangePassword_RevokesRefreshTokens_TransactionBoundary()
    {
        var email = "auth_changepass@orimate.com";
        var password = "OldPassword123!";
        await _client.PostAsJsonAsync("/api/auth/register", new { email, password, displayName = "User" });

        var user = await _dbContext.Users.FirstAsync(u => u.Email == email);
        user.Status = AccountStatus.Active;
        await _dbContext.SaveChangesAsync();

        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var authData = await loginRes.Content.ReadFromJsonAsync<AuthResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authData!.Token);

        var changeReq = new { currentPassword = password, newPassword = "NewPassword456!", confirmPassword = "NewPassword456!" };
        var response = await _client.PostAsJsonAsync("/api/auth/change-password", changeReq);

        response.EnsureSuccessStatusCode();
        _dbContext.ChangeTracker.Clear();
        var dbUser = await _dbContext.Users.FirstAsync(u => u.Email == email);
        dbUser.RefreshTokenHash.Should().BeNull();
    }
}
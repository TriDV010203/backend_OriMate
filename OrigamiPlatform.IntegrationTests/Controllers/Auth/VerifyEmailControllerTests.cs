using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Commands.Auth;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.IntegrationTests;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Controllers.Auth;

public class VerifyEmailControllerTests : IntegrationTestBase
{
    public VerifyEmailControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ResendVerification_WithUnverifiedUser_ShouldGenerateNewToken()
    {
        var email = "resend_test@origami.com";
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = "Hash",
            Status = OrigamiPlatform.Domain.Enums.AccountStatus.Unverified,
            VerificationToken = "OLD_TOKEN_123",
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.Users.AddAsync(testUser);
        await _dbContext.SaveChangesAsync();

        var requestPayload = new { Email = email };

        var response = await _client.PostAsJsonAsync("/api/auth/resend-verification", requestPayload);

        response.IsSuccessStatusCode.Should().BeTrue("API Resend Verification phải trả về 200 OK");

        var userInDb = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        userInDb!.VerificationToken.Should().NotBeNullOrEmpty("Hệ thống phải cấp Verification Token mới");
        userInDb.VerificationToken.Should().NotBe("OLD_TOKEN_123", "Token mới phải khác token cũ");
    }

    [Fact]
    public async Task ResendVerification_WithAlreadyActiveUser_ShouldReturnBadRequest()
    {
        var email = "already_active@origami.com";
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = "Hash",
            Status = OrigamiPlatform.Domain.Enums.AccountStatus.Active, 
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.Users.AddAsync(testUser);
        await _dbContext.SaveChangesAsync();

        var response = await _client.PostAsJsonAsync("/api/auth/resend-verification", new { Email = email });

        response.IsSuccessStatusCode.Should().BeFalse("Không được phép gửi lại mail kích hoạt cho tài khoản đã Active");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }


    [Fact]
    public async Task VerifyEmail_WithValidToken_ShouldActivateUser()
    {
        var email = "unverified@origami.com";
        var token = "VALID_TOKEN_12345";
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = "Hash",
            Status = OrigamiPlatform.Domain.Enums.AccountStatus.Unverified,
            VerificationToken = token,
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.Users.AddAsync(testUser);
        await _dbContext.SaveChangesAsync();

        var response = await _client.GetAsync($"/api/auth/verify-email?token={token}");

        response.IsSuccessStatusCode.Should().BeTrue("API phải trả về 200 OK khi token hợp lệ");

        var userInDb = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);

        userInDb!.Status.Should().Be(OrigamiPlatform.Domain.Enums.AccountStatus.Active);
        userInDb.VerificationToken.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task VerifyEmail_WithInvalidToken_ShouldReturnBadRequest()
    {
        var email = "invalid_token@origami.com";
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = "Hash",
            Status = OrigamiPlatform.Domain.Enums.AccountStatus.Unverified,
            VerificationToken = "REAL_TOKEN_999",
            CreatedAt = DateTime.UtcNow 
        };
        await _dbContext.Users.AddAsync(testUser);
        await _dbContext.SaveChangesAsync();

        var response = await _client.GetAsync("/api/auth/verify-email?token=WRONG_TOKEN_000");

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);

        var userInDb = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        userInDb!.Status.Should().Be(OrigamiPlatform.Domain.Enums.AccountStatus.Unverified);
    }

    [Fact]
    public async Task ResendVerification_ShouldInvalidatePreviousUnusedToken()
    {
        var email = "invalidate_old_token@origami.com";
        var oldToken = "OLD_VERIFICATION_TOKEN_ABC";
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = "Hash",
            Status = AccountStatus.Unverified,
            VerificationToken = oldToken,
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.Users.AddAsync(testUser);
        await _dbContext.SaveChangesAsync();

        var resendResponse = await _client.PostAsJsonAsync("/api/auth/resend-verification", new { Email = email });
        resendResponse.IsSuccessStatusCode.Should().BeTrue("Yêu cầu gửi lại mã xác thực thành công");

        var verifyOldResponse = await _client.GetAsync($"/api/auth/verify-email?token={oldToken}");

        verifyOldResponse.IsSuccessStatusCode.Should().BeFalse("Token xác thực cũ phải mất hiệu lực ngay sau khi yêu cầu gửi mã mới (FT-02)");
        verifyOldResponse.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);

        var userInDb = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        userInDb!.Status.Should().Be(AccountStatus.Unverified);
        userInDb.VerificationToken.Should().NotBe(oldToken, "Token cũ trong DB phải được thay thế bằng token mới sinh");
        userInDb.VerificationToken.Should().NotBeNullOrEmpty();
    }
}
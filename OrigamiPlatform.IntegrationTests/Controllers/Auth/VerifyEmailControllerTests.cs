using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Commands.Auth;
using OrigamiPlatform.Domain.Entities;
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
    public async Task VerifyEmail_WithValidToken_ShouldActivateUser()
    {
        // GIVEN
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

        // WHEN: Gọi API Verify dưới dạng GET với Query Parameter (?token=...)
        var response = await _client.GetAsync($"/api/auth/verify-email?token={token}");

        // THEN
        response.IsSuccessStatusCode.Should().BeTrue("API phải trả về 200 OK khi token hợp lệ");

        var userInDb = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        userInDb!.Status.Should().Be(OrigamiPlatform.Domain.Enums.AccountStatus.Active);
        userInDb.VerificationToken.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task VerifyEmail_WithInvalidToken_ShouldReturnBadRequest()
    {
        // GIVEN
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

        // WHEN: Gửi token sai qua query parameter
        var response = await _client.GetAsync("/api/auth/verify-email?token=WRONG_TOKEN_000");

        // THEN
        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);

        var userInDb = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        userInDb!.Status.Should().Be(OrigamiPlatform.Domain.Enums.AccountStatus.Unverified);
    }
}
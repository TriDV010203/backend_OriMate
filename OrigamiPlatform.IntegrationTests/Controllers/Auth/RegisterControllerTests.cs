using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.Auth;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.IntegrationTests;
using System.Net.Http.Json;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Controllers.Auth;

public class RegisterControllerTests : IntegrationTestBase
{
    public RegisterControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Register_ValidData_ReturnsSuccessAndPersistsUser()
    {
        var request = new RegisterRequest("newuser@origami.com", "StrongPassword123!", "Origami Master");

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.IsSuccessStatusCode.Should().BeTrue();

        var userInDb = await _dbContext.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        userInDb.Should().NotBeNull();
        userInDb!.PasswordHash.Should().NotBeNullOrEmpty();
        userInDb.PasswordHash.Should().NotBe(request.Password);
        userInDb.Profile?.DisplayName.Should().Be(request.DisplayName);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        var email = "duplicate@origami.com";
        await _dbContext.Users.AddAsync(new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = "ExistingHash",
            Status = OrigamiPlatform.Domain.Enums.AccountStatus.Active,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        var request = new RegisterRequest(email, "ValidPassword123!", "Another Guy");

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.IsSuccessStatusCode.Should().BeFalse();
        var count = await _dbContext.Users.CountAsync(u => u.Email == email);
        count.Should().Be(1);
    }

    [Fact]
    public async Task Register_InvalidPassword_ReturnsBadRequest()
    {
        var request = new RegisterRequest("weakpass@origami.com", "123", "Weak User");

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.IsSuccessStatusCode.Should().BeFalse();
    }

    [Theory]
    [InlineData("Pass123!", true)]
    [InlineData("P@ssword123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890", true)] 
    [InlineData("P@ssword1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901", false)]
    public async Task Register_PasswordLength_BoundaryValueAnalysis(string password, bool shouldSucceed)
    {
        var email = $"bva_pass_{Guid.NewGuid()}@origami.com";
        var request = new RegisterRequest(email, password, "BVA User");

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.IsSuccessStatusCode.Should().Be(shouldSucceed, $"Mật khẩu độ dài {password.Length} phải trả về Success = {shouldSucceed} (BV-01)");
    }
}
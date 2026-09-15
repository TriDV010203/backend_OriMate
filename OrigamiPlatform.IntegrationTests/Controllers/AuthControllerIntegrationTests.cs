using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrigamiPlatform.Application.DTOs.Auth;

namespace OrigamiPlatform.IntegrationTests.Controllers;

public sealed class AuthControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthControllerIntegrationTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Register_ValidPayload_ReturnsOkAndToken()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = "register@example.com",
            Password = "Password@123",
            DisplayName = "Integration User"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        result.Should().NotBeNull();
        result!.Email.Should().Be("register@example.com");
        result.Token.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient();
        var request = new
        {
            Email = "duplicate@example.com",
            Password = "Password@123",
            DisplayName = "Duplicate User"
        };

        (await client.PostAsJsonAsync("/api/auth/register", request)).StatusCode
            .Should().Be(HttpStatusCode.OK);
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.Error.Should().Contain("already registered");
    }

    [Fact]
    public async Task Login_UnverifiedAccount_ReturnsForbidden()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient();
        const string email = "unverified@example.com";
        const string password = "Password@123";

        (await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = password,
            DisplayName = "Unverified User"
        })).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.Error.Should().Contain("verify your email");
    }

    private sealed record ErrorResponse(string Error);
}

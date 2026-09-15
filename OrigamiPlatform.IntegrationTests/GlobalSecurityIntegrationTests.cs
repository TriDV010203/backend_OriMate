using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrigamiPlatform.Application.DTOs.Auth;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Infrastructure.Persistence;

namespace OrigamiPlatform.IntegrationTests;

public sealed class GlobalSecurityIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public GlobalSecurityIntegrationTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Unauthenticated_AccessToProtectedEndpoint_ReturnsUnauthorized()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/gamification/skill-level");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Forbidden_AccessToAdminEndpointWithStandardUserRole_ReturnsForbidden()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient();
        const string email = "standard-user-security@example.com";
        const string password = "Password@123";

        var registration = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = password,
            DisplayName = "Standard Security User"
        });
        var registeredUser = await registration.Content.ReadFromJsonAsync<AuthResponse>();
        registeredUser.Should().NotBeNull();

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(candidate => candidate.Id == registeredUser!.UserId);
            user.Status = AccountStatus.Active;
            await db.SaveChangesAsync();
        }

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = password
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var authenticatedUser = await login.Content.ReadFromJsonAsync<AuthResponse>();
        authenticatedUser.Should().NotBeNull();
        authenticatedUser!.Roles.Should().NotContain("Admin");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authenticatedUser.Token);

        var response = await client.GetAsync("/api/admin/categories");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
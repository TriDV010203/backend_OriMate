using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Infrastructure.Persistence;

namespace OrigamiPlatform.IntegrationTests.Controllers;

public sealed class AdminManagementIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string JwtKey = "origami-platform-integration-test-secret-key";
    private readonly CustomWebApplicationFactory _factory;

    public AdminManagementIntegrationTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task ApproveTutorial_PendingReviewState_ByManager_ReturnsOk_AndChangesStatusToPublished()
    {
        var data = await SeedDataAsync();
        using var client = CreateClient(data.Manager, UserRoleType.Manager);

        var response = await client.PutAsync($"/api/tutorials/{data.PendingTutorial.Id}/publish", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadTutorialStatusAsync(data.PendingTutorial.Id)).Should().Be(TutorialStatus.Published);
    }

    [Fact]
    public async Task RejectTutorial_PendingReviewState_ByManager_ReturnsOk_AndChangesStatusToDraft()
    {
        var data = await SeedDataAsync();
        using var client = CreateClient(data.Manager, UserRoleType.Manager);
        var request = new { reason = "Please add clearer folding steps." };

        var response = await client.PutAsJsonAsync($"/api/tutorials/{data.PendingTutorial.Id}/reject", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadTutorialStatusAsync(data.PendingTutorial.Id)).Should().Be(TutorialStatus.RevisionRequired);
    }

    [Fact]
    public async Task ApproveTutorial_DraftState_ReturnsBadRequest()
    {
        var data = await SeedDataAsync();
        using var client = CreateClient(data.Manager, UserRoleType.Manager);

        var response = await client.PutAsync($"/api/tutorials/{data.DraftTutorial.Id}/publish", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ApproveTutorial_AlreadyPublishedState_ReturnsBadRequest()
    {
        var data = await SeedDataAsync();
        using var client = CreateClient(data.Manager, UserRoleType.Manager);

        var response = await client.PutAsync($"/api/tutorials/{data.PublishedTutorial.Id}/publish", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ApproveTutorial_ByCustomerRole_ReturnsForbidden()
    {
        var data = await SeedDataAsync();
        using var client = CreateClient(data.Customer, UserRoleType.User);

        var response = await client.PutAsync($"/api/tutorials/{data.PendingTutorial.Id}/publish", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ApproveTutorial_ByCreatorRole_ReturnsForbidden()
    {
        var data = await SeedDataAsync();
        using var client = CreateClient(data.Creator, UserRoleType.User);

        var response = await client.PutAsync($"/api/tutorials/{data.PendingTutorial.Id}/publish", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUsers_Pagination_ValidPageAndSize_ReturnsOk()
    {
        var data = await SeedDataAsync();
        using var client = CreateClient(data.Admin, UserRoleType.Admin);

        var response = await client.GetAsync("/api/admin/users?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SuspendUser_ValidReason_ByAdmin_ReturnsOk_AndChangesAccountStatus()
    {
        var data = await SeedDataAsync();
        using var client = CreateClient(data.Admin, UserRoleType.Admin);

        var response = await client.PutAsJsonAsync(
            $"/api/admin/users/{data.Customer.Id}/suspend",
            new { reason = "Repeated violations of community rules." });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadUserStatusAsync(data.Customer.Id)).Should().Be(AccountStatus.Suspended);
    }

    [Fact]
    public async Task SuspendUser_MissingReason_ReturnsBadRequest()
    {
        var data = await SeedDataAsync();
        using var client = CreateClient(data.Admin, UserRoleType.Admin);

        var response = await client.PutAsJsonAsync(
            $"/api/admin/users/{data.Customer.Id}/suspend",
            new { reason = string.Empty });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadUserStatusAsync(data.Customer.Id)).Should().Be(AccountStatus.Active);
    }

    [Fact]
    public async Task SuspendUser_AlreadySuspendedUser_ReturnsBadRequest()
    {
        var data = await SeedDataAsync();
        using var client = CreateClient(data.Admin, UserRoleType.Admin);

        var response = await client.PutAsJsonAsync(
            $"/api/admin/users/{data.SuspendedUser.Id}/suspend",
            new { reason = "Repeated violations of community rules." });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SuspendUser_ByManagerRole_ReturnsForbidden()
    {
        var data = await SeedDataAsync();
        using var client = CreateClient(data.Manager, UserRoleType.Manager);

        var response = await client.PutAsJsonAsync(
            $"/api/admin/users/{data.Customer.Id}/suspend",
            new { reason = "Repeated violations of community rules." });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ReadUserStatusAsync(data.Customer.Id)).Should().Be(AccountStatus.Active);
    }

    private HttpClient CreateClient(User user, UserRoleType role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(user, role));
        return client;
    }

    private static string CreateToken(User user, UserRoleType role)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, role.ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "OrigamiPlatform",
            audience: "OrigamiPlatform",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<TutorialStatus> ReadTutorialStatusAsync(Guid tutorialId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Tutorials.Where(t => t.Id == tutorialId).Select(t => t.Status).SingleAsync();
    }

    private async Task<AccountStatus> ReadUserStatusAsync(Guid userId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Users.Where(u => u.Id == userId).Select(u => u.Status).SingleAsync();
    }

    private async Task<SeedState> SeedDataAsync()
    {
        await _factory.ResetDatabaseAsync();
        var now = DateTime.UtcNow;
        var admin = NewUser("admin");
        var manager = NewUser("manager");
        var creator = NewUser("creator");
        var customer = NewUser("customer");
        var suspendedUser = NewUser("suspended", AccountStatus.Suspended);
        AddRole(admin, UserRoleType.Admin, now);
        AddRole(manager, UserRoleType.Manager, now);
        AddRole(creator, UserRoleType.User, now);
        AddRole(customer, UserRoleType.User, now);
        AddRole(suspendedUser, UserRoleType.User, now);
        var category = new Category
        {
            Id = 9001,
            Name = "Integration Tests",
            IsActive = true,
            CreatedAt = now
        };
        var pending = NewTutorial("pending-review", creator, category, TutorialStatus.PendingManagerReview, now);
        var draft = NewTutorial("draft", creator, category, TutorialStatus.Draft, now);
        var published = NewTutorial("published", creator, category, TutorialStatus.Published, now);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.AddRange(admin, manager, creator, customer, suspendedUser);
        db.Categories.Add(category);
        db.Tutorials.AddRange(pending, draft, published);
        await db.SaveChangesAsync();
        return new SeedState(admin, manager, creator, customer, suspendedUser, pending, draft, published);
    }

    private static User NewUser(string name, AccountStatus status = AccountStatus.Active) => new()
    {
        Id = Guid.NewGuid(),
        Email = $"{name}-{Guid.NewGuid():N}@example.com",
        PasswordHash = "unused",
        Status = status,
        CreatedAt = DateTime.UtcNow
    };

    private static void AddRole(User user, UserRoleType role, DateTime createdAt)
        => user.Roles.Add(new UserRole { UserId = user.Id, Role = role, CreatedAt = createdAt });

    private static Tutorial NewTutorial(
        string slug,
        User author,
        Category category,
        TutorialStatus status,
        DateTime createdAt) => new()
        {
            Id = Guid.NewGuid(),
            AuthorId = author.Id,
            Author = author,
            CategoryId = category.Id,
            Category = category,
            Title = $"Integration {slug}",
            Description = "A tutorial used by integration tests.",
            Slug = $"{slug}-{Guid.NewGuid():N}",
            Type = TutorialType.Free,
            Difficulty = TutorialDifficulty.Beginner,
            Status = status,
            CreatedAt = createdAt
        };

    private sealed record SeedState(
        User Admin,
        User Manager,
        User Creator,
        User Customer,
        User SuspendedUser,
        Tutorial PendingTutorial,
        Tutorial DraftTutorial,
        Tutorial PublishedTutorial);
}

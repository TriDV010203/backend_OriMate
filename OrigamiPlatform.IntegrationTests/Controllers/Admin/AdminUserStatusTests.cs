using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.Auth;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Controllers.Admin;

public class AdminUserStatusTests : IntegrationTestBase
{
    public AdminUserStatusTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<string> AuthenticateAsAdminAsync()
    {
        var adminEmail = "admin_status_test@origami.com";
        var rawPassword = "AdminPassword123!";
        var existingAdmin = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        if (existingAdmin == null)
        {
            var adminUser = new User { Id = Guid.NewGuid(), Email = adminEmail, PasswordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword), Status = AccountStatus.Active, CreatedAt = DateTime.UtcNow };
            await _dbContext.Users.AddAsync(adminUser);
            await _dbContext.UserRoles.AddAsync(new UserRole { UserId = adminUser.Id, Role = UserRoleType.Admin });
            await _dbContext.SaveChangesAsync();
        }
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(adminEmail, rawPassword));
        var authData = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return authData!.Token;
    }

    [Fact]
    public async Task SuspendUser_ByAdmin_ShouldChangeUserStatusToSuspended()
    {
        var adminToken = await AuthenticateAsAdminAsync();
        var targetUser = new User { Id = Guid.NewGuid(), Email = "to_be_suspended@origami.com", PasswordHash = "Hash", Status = AccountStatus.Active, CreatedAt = DateTime.UtcNow };
        await _dbContext.Users.AddAsync(targetUser);
        await _dbContext.SaveChangesAsync();

        var requestUrl = $"/api/admin/users/{targetUser.Id}/suspend";
        var requestMessage = new HttpRequestMessage(HttpMethod.Put, requestUrl);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        requestMessage.Content = JsonContent.Create(new { Reason = "Vi phạm chính sách" });

        var response = await _client.SendAsync(requestMessage);

        response.IsSuccessStatusCode.Should().BeTrue();
        var updatedUser = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == targetUser.Id);
        updatedUser!.Status.Should().Be(AccountStatus.Suspended);
    }

    [Fact]
    public async Task ActivateUser_ByAdmin_ShouldChangeUserStatusToActive()
    {
        var adminToken = await AuthenticateAsAdminAsync();
        var suspendedUser = new User { Id = Guid.NewGuid(), Email = "suspended_user@origami.com", PasswordHash = "Hash", Status = AccountStatus.Suspended, CreatedAt = DateTime.UtcNow };
        await _dbContext.Users.AddAsync(suspendedUser);
        await _dbContext.SaveChangesAsync();

        var requestUrl = $"/api/admin/users/{suspendedUser.Id}/activate";
        var requestMessage = new HttpRequestMessage(HttpMethod.Put, requestUrl);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        requestMessage.Content = JsonContent.Create(new { });

        var response = await _client.SendAsync(requestMessage);

        response.IsSuccessStatusCode.Should().BeTrue();
        var updatedUser = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == suspendedUser.Id);
        updatedUser!.Status.Should().Be(AccountStatus.Active);
    }

    [Fact]
    public async Task SuspendUser_AlreadySuspendedUser_ShouldReturnBadRequest()
    {
        var adminToken = await AuthenticateAsAdminAsync();
        var suspendedUser = new User { Id = Guid.NewGuid(), Email = "already_banned@origami.com", PasswordHash = "Hash", Status = AccountStatus.Suspended, CreatedAt = DateTime.UtcNow };
        await _dbContext.Users.AddAsync(suspendedUser);
        await _dbContext.SaveChangesAsync();

        var requestUrl = $"/api/admin/users/{suspendedUser.Id}/suspend";
        var requestMessage = new HttpRequestMessage(HttpMethod.Put, requestUrl);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        requestMessage.Content = JsonContent.Create(new { Reason = "Double ban" });

        var response = await _client.SendAsync(requestMessage);

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SuspendUser_TargetIsAdmin_ShouldBeRejectedWithForbiddenOrBadRequest()
    {
        var adminToken = await AuthenticateAsAdminAsync(); 

        var targetAdminEmail = "target_admin_banned@origami.com";
        var targetAdmin = new User
        {
            Id = Guid.NewGuid(),
            Email = targetAdminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass123!"),
            Status = AccountStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.Users.AddAsync(targetAdmin);
        await _dbContext.UserRoles.AddAsync(new UserRole { UserId = targetAdmin.Id, Role = UserRoleType.Admin });
        await _dbContext.SaveChangesAsync();

        var requestUrl = $"/api/admin/users/{targetAdmin.Id}/suspend";
        var requestMessage = new HttpRequestMessage(HttpMethod.Put, requestUrl);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        requestMessage.Content = JsonContent.Create(new { Reason = "Cố tình khóa Admin khác" });

        var response = await _client.SendAsync(requestMessage);

        response.IsSuccessStatusCode.Should().BeFalse("Tài khoản Admin không bao giờ được phép bị khóa bởi bất kỳ ai");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.BadRequest);

        var userInDb = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == targetAdmin.Id);
        userInDb!.Status.Should().Be(AccountStatus.Active);
    }
}
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

public class AdminRoleManagementTests : IntegrationTestBase
{
    public AdminRoleManagementTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<string> AuthenticateAsAdminAsync()
    {
        var adminEmail = "admin_role_test@origami.com";
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
    public async Task AssignRole_ByAdmin_ShouldSucceedAndPersist()
    {
        var adminToken = await AuthenticateAsAdminAsync();
        var targetUser = new User { Id = Guid.NewGuid(), Email = "assign_role_user@origami.com", PasswordHash = "Hash", Status = AccountStatus.Active, CreatedAt = DateTime.UtcNow };
        await _dbContext.Users.AddAsync(targetUser);
        await _dbContext.SaveChangesAsync();

        var requestUrl = $"/api/admin/users/{targetUser.Id}/assign-role";

        var requestMessage = new HttpRequestMessage(HttpMethod.Put, requestUrl);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        requestMessage.Content = JsonContent.Create(new
        {
            Role = UserRoleType.ContributorReviewer.ToString()
        });

        var response = await _client.SendAsync(requestMessage);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"API Failed with Status: {response.StatusCode}. Route tried: {requestUrl}. Body sent: UserId={targetUser.Id}, Role={UserRoleType.ContributorReviewer.ToString()}. Backend Error: {errorBody}");
        }

        response.IsSuccessStatusCode.Should().BeTrue();
        var userRoleInDb = await _dbContext.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == targetUser.Id && ur.Role == UserRoleType.ContributorReviewer);
        userRoleInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task RemoveRole_ByAdmin_ShouldDeleteFromDb()
    {
        var adminToken = await AuthenticateAsAdminAsync();
        var targetUser = new User { Id = Guid.NewGuid(), Email = "remove_role@origami.com", PasswordHash = "Hash", Status = AccountStatus.Active };
        await _dbContext.Users.AddAsync(targetUser);
        await _dbContext.UserRoles.AddAsync(new UserRole { UserId = targetUser.Id, Role = UserRoleType.Manager });
        await _dbContext.SaveChangesAsync();

        var requestUrl = $"/api/admin/users/{targetUser.Id}/remove-role";

        var requestMessage = new HttpRequestMessage(HttpMethod.Delete, requestUrl);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        requestMessage.Content = JsonContent.Create(new
        {
            Role = UserRoleType.Manager.ToString()
        });

        var response = await _client.SendAsync(requestMessage);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"API Failed with Status: {response.StatusCode}. Route tried: {requestUrl}. Backend Error: {errorBody}");
        }

        response.IsSuccessStatusCode.Should().BeTrue();
        var userRoleInDb = await _dbContext.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == targetUser.Id && ur.Role == UserRoleType.Manager);
        userRoleInDb.Should().BeNull();
    }

    [Fact]
    public async Task AssignRole_UserAlreadyHasRole_ShouldReturnSuccessOrNotModified()
    {
        // GIVEN: User ĐÃ CÓ sẵn role Manager (Test tính Idempotency)
        var adminToken = await AuthenticateAsAdminAsync();
        var targetUser = new User { Id = Guid.NewGuid(), Email = "already_manager@origami.com", PasswordHash = "Hash", Status = AccountStatus.Active };
        await _dbContext.Users.AddAsync(targetUser);
        await _dbContext.UserRoles.AddAsync(new UserRole { UserId = targetUser.Id, Role = UserRoleType.Manager });
        await _dbContext.SaveChangesAsync();

        // WHEN: Admin tiếp tục gọi API gán role Manager cho user này
        var requestUrl = $"/api/admin/users/{targetUser.Id}/assign-role";
        var requestMessage = new HttpRequestMessage(HttpMethod.Put, requestUrl);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        requestMessage.Content = JsonContent.Create(new { Role = UserRoleType.Manager.ToString() });

        var response = await _client.SendAsync(requestMessage);

        // THEN
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError, "Hệ thống không được crash do lỗi trùng lặp dữ liệu (Idempotency)");

        var rolesCount = await _dbContext.UserRoles.CountAsync(ur => ur.UserId == targetUser.Id && ur.Role == UserRoleType.Manager);
        rolesCount.Should().Be(1, "Dù gọi API bao nhiêu lần, hệ thống chỉ lưu đúng 1 record cho mỗi Role tương ứng của User");
    }

    [Fact]
    public async Task AssignRole_ByAdmin_ShouldGenerateAuditLogEntry()
    {
        // GIVEN
        var adminToken = await AuthenticateAsAdminAsync();
        var targetUser = new User { Id = Guid.NewGuid(), Email = "audit_role_user@origami.com", PasswordHash = "Hash", Status = AccountStatus.Active };
        await _dbContext.Users.AddAsync(targetUser);
        await _dbContext.SaveChangesAsync();

        // WHEN: Cấp quyền
        var requestUrl = $"/api/admin/users/{targetUser.Id}/assign-role";
        var requestMessage = new HttpRequestMessage(HttpMethod.Put, requestUrl);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        requestMessage.Content = JsonContent.Create(new { Role = UserRoleType.ContributorReviewer.ToString() });

        await _client.SendAsync(requestMessage);

        // THEN: Kiểm tra sự tồn tại của Audit Log (BR-ADMIN-01)
        var auditLogInDb = await _dbContext.AuditLogs
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();

        auditLogInDb.Should().NotBeNull("Mọi thao tác gán quyền của Admin phải được ghi vết (BR-ADMIN-01)");
    }
}
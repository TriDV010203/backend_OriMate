using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Services.Admin_ConfigService;

public class AdminUsersControllerIntegrationTests : IntegrationTestBase
{
    public AdminUsersControllerIntegrationTests(CustomWebApplicationFactory factory) : base(factory) { }

    // 🔬 Coverage Technique: Happy Path (Admin creates managed user successfully)
    [Fact]
    public async Task CreateUserByAdmin_AsAdmin_ReturnsSuccess_CreatesActiveUser_HappyPath()
    {
        await AuthenticateAsAsync("Admin");
        var req = new
        {
            email = $"admin_created_{Guid.NewGuid().ToString()[..5]}@orimate.com",
            password = "Password123!",
            displayName = "Managed User",
            role = "Manager"
        };

        var response = await _client.PostAsJsonAsync("/api/admin/users", req);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("email").GetString().Should().Be(req.email);
        result.GetProperty("status").GetString().Should().Be("Active");

        _dbContext.ChangeTracker.Clear();
        var dbUser = await _dbContext.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Email == req.email);
        dbUser.Should().NotBeNull();
        dbUser!.Status.Should().Be(AccountStatus.Active);
        dbUser.Roles.Any(r => r.Role == UserRoleType.Manager).Should().BeTrue();
    }

    // 🔬 Coverage Technique: Error Path (Non-admin prohibited from creating admin-level users)
    [Fact]
    public async Task CreateUserByAdmin_AsNonAdmin_ReturnsForbidden_ErrorPath()
    {
        await AuthenticateAsAsync("User");
        var req = new
        {
            email = "unauth_create@orimate.com",
            password = "Password123!",
            displayName = "Bad User",
            role = "Admin"
        };

        var response = await _client.PostAsJsonAsync("/api/admin/users", req);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // 🔬 Coverage Technique: Boundary Value (Testing password length constraints lower boundary)
    [Fact]
    public async Task CreateUserByAdmin_ShortPassword_ReturnsBadRequest_BoundaryError()
    {
        await AuthenticateAsAsync("Admin");
        var req = new
        {
            email = "short_pass@orimate.com",
            password = "123", // Below minimum length boundary
            displayName = "Short Pass User",
            role = "User"
        };

        var response = await _client.PostAsJsonAsync("/api/admin/users", req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // 🔬 Coverage Technique: Happy Path (Admin assigns role successfully)
    [Fact]
    public async Task AssignRole_ToValidUser_UpdatesRolesSuccessfully()
    {
        await AuthenticateAsAsync("Admin");

        var targetUserId = Guid.NewGuid();
        var targetUser = new Domain.Entities.User
        {
            Id = targetUserId,
            Email = $"role_target_{Guid.NewGuid().ToString()[..5]}@orimate.com",
            PasswordHash = "hashed",
            Status = AccountStatus.Active
        };
        _dbContext.Users.Add(targetUser);
        await _dbContext.SaveChangesAsync();

        var assignReq = new { role = "ContributorReviewer" };

        var response = await _client.PutAsJsonAsync($"/api/admin/users/{targetUserId}/assign-role", assignReq);

        response.EnsureSuccessStatusCode();

        _dbContext.ChangeTracker.Clear();
        var dbUser = await _dbContext.Users.Include(u => u.Roles).FirstAsync(u => u.Id == targetUserId);
        dbUser.Roles.Any(r => r.Role == UserRoleType.ContributorReviewer).Should().BeTrue();
    }

    // 🔬 Coverage Technique: Error Path / Transaction Boundary (Security guard preventing self-demotion of Admin role)
    [Fact]
    public async Task AssignRole_RemoveOwnAdminRole_ReturnsBadRequest_SecurityConstraint()
    {
        // Arrange: Đăng ký user mang quyền Admin qua helper
        var adminId = await AuthenticateAsAsync("Admin");

        // Khắc phục: Chủ động gán bản ghi UserRole (Admin) vào DB cho đúng với ngữ cảnh bảo mật
        _dbContext.UserRoles.Add(new OrigamiPlatform.Domain.Entities.UserRole
        {
            UserId = adminId,
            Role = UserRoleType.Admin,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        var assignReq = new { role = "User" };

        // Act: Admin tự thao tác hạ quyền của chính mình xuống "User"
        var response = await _client.PutAsJsonAsync($"/api/admin/users/{adminId}/assign-role", assignReq);

        // Assert: Phải trả về 400 BadRequest theo đúng logic trong AssignRoleHandler
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // 🔬 Coverage Technique: Happy Path (Suspending an active user account)
    [Fact]
    public async Task SuspendUser_ActiveUser_SetsStatusToSuspended_HappyPath()
    {
        await AuthenticateAsAsync("Admin");
        var targetUserId = Guid.NewGuid();
        var targetUser = new Domain.Entities.User
        {
            Id = targetUserId,
            Email = $"suspend_target_{Guid.NewGuid().ToString()[..5]}@orimate.com",
            PasswordHash = "hashed",
            Status = AccountStatus.Active
        };
        _dbContext.Users.Add(targetUser);
        await _dbContext.SaveChangesAsync();

        var suspendReq = new { reason = "Violating community standards repeatedly." };

        var response = await _client.PutAsJsonAsync($"/api/admin/users/{targetUserId}/suspend", suspendReq);

        response.EnsureSuccessStatusCode();

        _dbContext.ChangeTracker.Clear();
        var dbUser = await _dbContext.Users.FirstAsync(u => u.Id == targetUserId);
        dbUser.Status.Should().Be(AccountStatus.Suspended);
    }

    // 🔬 Coverage Technique: Error Path / Transaction Boundary (Security constraint: Admin cannot suspend another Admin - BR-ADMIN-02)
    [Fact]
    public async Task SuspendUser_AdminAccount_ReturnsForbidden_SecurityConstraint_BR_ADMIN_02()
    {
        await AuthenticateAsAsync("Admin");
        var otherAdminId = Guid.NewGuid();
        var otherAdmin = new Domain.Entities.User
        {
            Id = otherAdminId,
            Email = $"other_admin_{Guid.NewGuid().ToString()[..5]}@orimate.com",
            PasswordHash = "hashed",
            Status = AccountStatus.Active,
            Roles = new List<Domain.Entities.UserRole>
            {
                new Domain.Entities.UserRole { UserId = otherAdminId, Role = UserRoleType.Admin }
            }
        };
        _dbContext.Users.Add(otherAdmin);
        await _dbContext.SaveChangesAsync();

        var suspendReq = new { reason = "Trying to suspend peer admin." };

        var response = await _client.PutAsJsonAsync($"/api/admin/users/{otherAdminId}/suspend", suspendReq);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
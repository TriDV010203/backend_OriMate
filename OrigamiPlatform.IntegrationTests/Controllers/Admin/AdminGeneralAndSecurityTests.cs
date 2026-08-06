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

public class AdminGeneralAndSecurityTests : IntegrationTestBase
{
    public AdminGeneralAndSecurityTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<string> AuthenticateAsAdminAsync()
    {
        var adminEmail = "admin_general_test@origami.com";
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
    public async Task CreateCategory_ByAdmin_ShouldPersist()
    {
        var adminToken = await AuthenticateAsAdminAsync();
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/admin/categories");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        requestMessage.Content = JsonContent.Create(new { Name = "Origami Cổ điển", Description = "Các mẫu truyền thống" });

        var response = await _client.SendAsync(requestMessage);

        response.IsSuccessStatusCode.Should().BeTrue();
        var categoryInDb = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Name == "Origami Cổ điển");
        categoryInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task AdminEndpoints_CalledByNormalUser_ShouldReturnForbidden()
    {
        var normalEmail = "not_an_admin@origami.com";
        var rawPassword = "Pass123!";
        var normalUser = new User { Id = Guid.NewGuid(), Email = normalEmail, PasswordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword), Status = AccountStatus.Active, CreatedAt = DateTime.UtcNow };
        await _dbContext.Users.AddAsync(normalUser);

        await _dbContext.SaveChangesAsync();

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(normalEmail, rawPassword));
        var authData = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/admin/categories");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authData!.Token);
        requestMessage.Content = JsonContent.Create(new { Name = "Hacked", Description = "Hacked" });

        var response = await _client.SendAsync(requestMessage);

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, "User bình thường gọi API admin phải trả về 403 Forbidden");
    }

    [Fact]
    public async Task CreateCategory_DuplicateName_ShouldReturnConflictOrBadRequest()
    {
        // GIVEN: Một Category đã tồn tại trong DB (Mô phỏng ràng buộc DC-05)
        var adminToken = await AuthenticateAsAdminAsync();
        var categoryName = "Origami Động vật";
        await _dbContext.Categories.AddAsync(new Category { Name = categoryName, IsActive = true });
        await _dbContext.SaveChangesAsync();

        // WHEN: Cố tình tạo thêm một Category với tên y hệt
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/admin/categories");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        requestMessage.Content = JsonContent.Create(new { Name = categoryName, Description = "Trùng tên" });

        var response = await _client.SendAsync(requestMessage);

        // THEN
        response.IsSuccessStatusCode.Should().BeFalse("Tên Category phải là duy nhất (DC-05)");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Conflict, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCategory_Deactivate_ShouldUpdateIsActiveFlag()
    {
        // GIVEN
        var adminToken = await AuthenticateAsAdminAsync();
        var category = new Category { Name = "To Be Deactivated", IsActive = true };
        await _dbContext.Categories.AddAsync(category);
        await _dbContext.SaveChangesAsync();

        // WHEN: Admin gọi API update để hủy kích hoạt category
        var requestMessage = new HttpRequestMessage(HttpMethod.Put, $"/api/admin/categories/{category.Id}");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        requestMessage.Content = JsonContent.Create(new { Name = "Deactivated Category", Description = "Updated", IsActive = false });

        var response = await _client.SendAsync(requestMessage);

        // THEN
        response.IsSuccessStatusCode.Should().BeTrue("Admin có quyền update và hủy kích hoạt Category");

        var catInDb = await _dbContext.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == category.Id);
        catInDb!.IsActive.Should().BeFalse("Cờ IsActive phải được cập nhật thành false dưới DB");
    }
}
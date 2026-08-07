using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.Auth;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Services.Auth_UserService;

public class UsersControllerIntegrationTests : IntegrationTestBase
{
    public UsersControllerIntegrationTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task CompleteUserOnboardingAndProfileWorkflow_Succeeds()
    {
        // Arrange: Đăng ký tài khoản và kích hoạt
        var email = "user_workflow@orimate.com";
        var password = "Password123!";
        await _client.PostAsJsonAsync("/api/auth/register", new { email, password, displayName = "Workflow User" });

        var dbUser = await _dbContext.Users.Include(u => u.Profile).FirstAsync(u => u.Email == email);
        dbUser.Status = AccountStatus.Active;

        // Chủ động tạo hoặc đảm bảo UserProfile tồn tại để Handler cập nhật thành công
        if (dbUser.Profile == null)
        {
            _dbContext.UserProfiles.Add(new Domain.Entities.UserProfile
            {
                UserId = dbUser.Id,
                DisplayName = "Workflow User",
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            dbUser.Profile.DisplayName = "Workflow User";
        }
        await _dbContext.SaveChangesAsync();

        // Login lấy Token thật
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        loginRes.EnsureSuccessStatusCode();
        var authData = await loginRes.Content.ReadFromJsonAsync<AuthResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authData!.Token);

        // 1. Kiểm tra trạng thái Onboarding ban đầu -> Phải là false
        var statusRes1 = await _client.GetAsync("/api/users/me/onboarding-status");
        statusRes1.EnsureSuccessStatusCode();
        var json1 = await statusRes1.Content.ReadFromJsonAsync<JsonElement>();

        bool isCompleted1 = json1.TryGetProperty("isCompleted", out var p1) ? p1.GetBoolean() :
                           (json1.TryGetProperty("IsCompleted", out var p1Alt) && p1Alt.GetBoolean());
        isCompleted1.Should().BeFalse();

        // 2. Gọi API hoàn thành Onboarding
        var completeRes = await _client.PostAsync("/api/users/me/complete-onboarding", null);
        completeRes.EnsureSuccessStatusCode();

        // 3. Kiểm tra lại trạng thái sau khi hoàn thành -> Phải là true
        var statusRes2 = await _client.GetAsync("/api/users/me/onboarding-status");
        statusRes2.EnsureSuccessStatusCode();
        var json2 = await statusRes2.Content.ReadFromJsonAsync<JsonElement>();

        bool isCompleted2 = json2.TryGetProperty("isCompleted", out var p2) ? p2.GetBoolean() :
                           (json2.TryGetProperty("IsCompleted", out var p2Alt) && p2Alt.GetBoolean());
        isCompleted2.Should().BeTrue();

        // 4. Cập nhật Profile cá nhân
        var profileReq = new { displayName = "Master Folder", avatarUrl = "https://img.com/avatar.jpg", bio = "Expert in origami." };
        var profileRes = await _client.PutAsJsonAsync("/api/users/profile", profileReq);
        profileRes.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetProfile_ReturnsCorrectData_ForAnonymous()
    {
        // Arrange: Tạo user qua helper và chủ động gán DisplayName tường minh trong UserProfiles
        var userId = await AuthenticateAsAsync("User");
        var user = await _dbContext.Users.Include(u => u.Profile).FirstAsync(u => u.Id == userId);

        if (user.Profile != null)
        {
            user.Profile.DisplayName = "Target Creator";
        }
        else
        {
            _dbContext.UserProfiles.Add(new Domain.Entities.UserProfile
            {
                UserId = userId,
                DisplayName = "Target Creator",
                CreatedAt = DateTime.UtcNow
            });
        }
        await _dbContext.SaveChangesAsync();

        // Xoá header Authorization để test chế độ ẩn danh (Anonymous)
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.GetAsync($"/api/users/{userId}/profile");

        // Assert
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        string displayName = json.TryGetProperty("displayName", out var val) ? val.GetString() ?? "" :
                            (json.TryGetProperty("DisplayName", out var valAlt) ? valAlt.GetString() ?? "" : "");
        displayName.Should().Be("Target Creator");
    }

    [Fact]
    public async Task GetTopCreators_ReturnsData_HappyPath()
    {
        var response = await _client.GetAsync("/api/users/top-creators?count=4");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync();
        result.Should().StartWith("[");
    }
}
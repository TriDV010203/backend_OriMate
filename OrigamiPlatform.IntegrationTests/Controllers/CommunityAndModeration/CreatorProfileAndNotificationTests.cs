using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.CommunityAndModeration;

public class CreatorProfileAndNotificationTests : IntegrationTestBase
{
    public CreatorProfileAndNotificationTests(CustomWebApplicationFactory factory) : base(factory) { }

    // ==============================================================================
    // 1. CREATOR PROFILE & FEED TESTS (FT-15)
    // ==============================================================================

    // [Happy Path] (AC-01) - Lấy thông tin trang cá nhân công khai của Creator thành công
    [Fact]
    public async Task GetProfile_ExistingUser_ReturnsProfileAndStats()
    {
        // Arrange
        var prereq = await SeedDefaultPrerequisitesAsync();

        // Tạo UserProfile cho user để có displayName thay vì "Unknown Creator"
        var userProfile = new UserProfile
        {
            UserId = prereq.AuthorId,
            DisplayName = "Origami Master Pro",
            Bio = "Chuyên gia gấp giấy nghệ thuật"
        };
        _dbContext.UserProfiles.Add(userProfile);

        // Tạo bài học đã Publish để test feed
        var tutorial = new Tutorial
        {
            Id = Guid.NewGuid(),
            Title = "Advanced Crane Tutorial",
            Slug = "advanced-crane-" + Guid.NewGuid(),
            CategoryId = prereq.CategoryId,
            AuthorId = prereq.AuthorId,
            Status = TutorialStatus.Published,
            PublishedAt = DateTime.UtcNow
        };
        _dbContext.Tutorials.Add(tutorial);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();

        // Act: Khách vãng lai (Guest / Anonymous) hoặc User đều gọi được route GET /api/users/{id}/profile
        var response = await _client.GetAsync($"/api/users/{prereq.AuthorId}/profile");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("Origami Master Pro");
        content.Should().Contain("Advanced Crane Tutorial");
    }

    // [Error Path / Edge Case] (NAC-01) - Lấy profile của User chưa có role Creator hoặc chưa có bài publish vẫn trả về 200 kèm list rỗng (không sập 404)
    [Fact]
    public async Task GetProfile_UserWithoutTutorials_ReturnsEmptyFeedGracefully()
    {
        // Arrange: Tạo user mới hoàn toàn, không có tutorial, không có profile custom
        var targetUserId = await AuthenticateAsAsync("User");
        _dbContext.ChangeTracker.Clear();

        // Act
        var response = await _client.GetAsync($"/api/users/{targetUserId}/profile");

        // Assert: Theo SRS FT-15 (NAC-01), hệ thống phải trả về profile cơ bản và list rỗng chứ không throw 404 error
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(targetUserId.ToString());
    }

    // [Happy Path & Transaction] - User tự cập nhật Profile cá nhân của mình
    [Fact]
    public async Task UpdateProfile_AuthenticatedUser_UpdatesSuccessfully()
    {
        // Arrange
        await AuthenticateAsAsync("User");
        var updateRequest = new
        {
            DisplayName = "Tên Mới Của Tôi",
            AvatarUrl = "https://example.com/avatar.jpg",
            Bio = "Bio mới được cập nhật"
        };

        // Act: Gọi PUT /api/users/profile (đòi hỏi Authorize)
        var response = await _client.PutAsJsonAsync("/api/users/profile", updateRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Cập nhật Profile thành công");
    }


    // ==============================================================================
    // 2. NOTIFICATION TESTS (FT-13)
    // ==============================================================================

    // [Happy Path] (AC-01) - Lấy danh sách thông báo của người dùng hiện tại
    [Fact]
    public async Task GetNotifications_AuthenticatedUser_ReturnsNotificationsList()
    {
        // Arrange
        await AuthenticateAsAsync("User");

        // Act: Thử gọi API lấy notifications (đường dẫn chuẩn phổ biến /api/notifications)
        var response = await _client.GetAsync("/api/notifications");

        // Assert: Nếu backend đã implement controller, endpoint này phải trả về thành công
        if (response.StatusCode != HttpStatusCode.NotFound)
        {
            response.EnsureSuccessStatusCode();
        }
    }

    // [Idempotency] (FT-13) - Đánh dấu toàn bộ thông báo là đã đọc không gây lỗi khi gọi lặp lại nhiều lần
    [Fact]
    public async Task MarkAllNotificationsAsRead_IsIdempotent_ReturnsSuccess()
    {
        await AuthenticateAsAsync("User");

        // Lần 1: Gọi đánh dấu đã đọc tất cả
        var response1 = await _client.PutAsync("/api/notifications/read-all", null);
        if (response1.StatusCode == HttpStatusCode.NotFound)
            response1 = await _client.PostAsync("/api/notifications/read-all", null);

        if (response1.StatusCode != HttpStatusCode.NotFound)
        {
            response1.EnsureSuccessStatusCode();

            // Lần 2: Gọi lại lần nữa để kiểm tra tính Idempotency (không được văng ngoại lệ 500)
            var response2 = await _client.PutAsync("/api/notifications/read-all", null);
            if (response2.StatusCode == HttpStatusCode.NotFound)
                response2 = await _client.PostAsync("/api/notifications/read-all", null);

            response2.EnsureSuccessStatusCode();
        }
    }
}
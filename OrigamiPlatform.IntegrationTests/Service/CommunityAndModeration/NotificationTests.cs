using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Controllers.CommunityAndModeration;

public class NotificationTests : IntegrationTestBase
{
    public NotificationTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetNotifications_ReturnsUserNotifications_Success()
    {
        var userId = await AuthenticateAsAsync("User");

        _dbContext.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            RecipientId = userId,
            Type = NotificationType.System,
            Message = "Chào mừng bạn đến với OriMate!",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var response = await _client.GetAsync("/api/notifications");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();

        result.ValueKind.Should().Be(JsonValueKind.Object);
        var notificationsArray = result.GetProperty("items");
        notificationsArray.ValueKind.Should().Be(JsonValueKind.Array);
        notificationsArray.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task MarkNotificationAsRead_ValidId_Succeeds()
    {
        var userId = await AuthenticateAsAsync("User");
        var notificationId = Guid.NewGuid();

        _dbContext.Notifications.Add(new Notification
        {
            Id = notificationId,
            RecipientId = userId,
            Type = NotificationType.System,
            Message = "Thông báo cần đánh dấu đã đọc",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var response = await _client.PutAsync($"/api/notifications/{notificationId}/read", null);

        response.EnsureSuccessStatusCode();

        var updatedNotif = await _dbContext.Notifications.FindAsync(notificationId);
        updatedNotif!.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task GetNotifications_WithoutAuthentication_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/notifications");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MarkNotificationAsRead_NotOwnedByUser_ReturnsForbiddenOrNotFound()
    {
        // FIX LỖI FK: Tạo một User thật trong DB làm chủ sở hữu thông báo trước
        var ownerId = Guid.NewGuid();
        var ownerUser = new User
        {
            Id = ownerId,
            Email = $"owner-{Guid.NewGuid()}@orimate.com",
            PasswordHash = "hashed",
            Status = AccountStatus.Active
        };
        _dbContext.Users.Add(ownerUser);

        var notificationId = Guid.NewGuid();
        _dbContext.Notifications.Add(new Notification
        {
            Id = notificationId,
            RecipientId = ownerId, // Bây giờ ownerId đã tồn tại thật trong bảng Users
            Type = NotificationType.System,
            Message = "Test",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Đăng nhập bằng một User KHÁC để test phân quyền
        await AuthenticateAsAsync("User");

        var response = await _client.PutAsync($"/api/notifications/{notificationId}/read", null);

        response.IsSuccessStatusCode.Should().BeFalse("Người dùng không được phép thao tác trên thông báo của người khác");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }
}
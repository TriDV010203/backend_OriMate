using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Controllers.CommunityAndModeration;

public class NotificationAndModerationTests : IntegrationTestBase
{
    public NotificationAndModerationTests(CustomWebApplicationFactory factory) : base(factory) { }

    // 🔬 Coverage Technique: Happy Path: Verify the primary success flow — DB state correct, events published, response correct (Notifications).
    [Fact]
    public async Task GetNotifications_ReturnsUserNotifications_Success()
    {
        // 1. Arrange
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

        // 2. Act
        var response = await _client.GetAsync("/api/notifications");

        // 3. Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();

        // API trả về Paginated Object chứa thuộc tính 'items' là một mảng
        result.ValueKind.Should().Be(JsonValueKind.Object);
        var notificationsArray = result.GetProperty("items");
        notificationsArray.ValueKind.Should().Be(JsonValueKind.Array);
        notificationsArray.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }

    // 🔬 Coverage Technique: Happy Path: Verify marking a single notification as read updates status correctly.
    [Fact]
    public async Task MarkNotificationAsRead_ValidId_Succeeds()
    {
        // 1. Arrange
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

        // 2. Act
        var response = await _client.PutAsync($"/api/notifications/{notificationId}/read", null);

        // 3. Assert
        response.EnsureSuccessStatusCode();

        var updatedNotif = await _dbContext.Notifications.FindAsync(notificationId);
        updatedNotif!.IsRead.Should().BeTrue();
    }

    // 🔬 Coverage Technique: Error Path: Verify failure scenarios — unauthenticated access rejection.
    [Fact]
    public async Task GetNotifications_WithoutAuthentication_ReturnsUnauthorized()
    {
        // 1. Arrange: Xóa token xác thực (Giả lập Guest)
        _client.DefaultRequestHeaders.Authorization = null;

        // 2. Act
        var response = await _client.GetAsync("/api/notifications");

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // 🔬 Coverage Technique: Happy Path: Verify Contributor Reviewer/Manager can delete a violating comment directly (FT-14 / BR-COMM-02).
    [Fact]
    public async Task DeleteViolatingComment_ByContributorReviewer_Succeeds()
    {
        // 1. Arrange: Đăng nhập với quyền ContributorReviewer (hoặc Admin/Manager được phép xóa vi phạm trực tiếp)
        // Lưu ý: Đảm bảo chuỗi role khớp với cấu hình role trong hệ thống seed/auth
        await AuthenticateAsAsync("Admin"); // Hoặc "ContributorReviewer" nếu hệ thống đã seed đúng role này

        // Lấy một userId hợp lệ đã được seed/tạo sẵn trong DB test
        var validAuthorId = await AuthenticateAsAsync("User");

        var commentId = Guid.NewGuid();
        _dbContext.Comments.Add(new Comment
        {
            Id = commentId,
            AuthorId = validAuthorId,
            TargetId = Guid.NewGuid(),
            TargetType = TargetType.CommunityPost,
            Content = "Bình luận vi phạm rõ ràng cần xóa.",
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Cần đăng nhập lại bằng quyền ContributorReviewer (hoặc Admin) trước khi gọi API xóa
        await AuthenticateAsAsync("Admin");

        var requestBody = new { Reason = "Bình luận mang nội dung đả kích, vi phạm quy chuẩn cộng đồng." };

        // 2. Act: Sử dụng HttpRequestMessage với HttpMethod.Delete để truyền [FromBody] chính xác
        var requestMessage = new HttpRequestMessage(HttpMethod.Delete, $"/api/moderation/comments/{commentId}")
        {
            Content = JsonContent.Create(requestBody)
        };

        var response = await _client.SendAsync(requestMessage);

        // 3. Assert
        response.EnsureSuccessStatusCode();

        var commentInDb = await _dbContext.Comments.FindAsync(commentId);
        commentInDb!.IsDeleted.Should().BeTrue();
    }

    // 🔬 Coverage Technique: Error Path: Verify role restriction — regular User cannot access direct moderation delete (FT-14 / BR-COMM-02).
    [Fact]
    public async Task DeleteViolatingComment_ByRegularUser_ReturnsForbidden()
    {
        // 1. Arrange: Đăng nhập tài khoản User thông thường (không có quyền kiểm duyệt)
        await AuthenticateAsAsync("User");

        var commentId = Guid.NewGuid();
        var requestBody = new { Reason = "Lý do xóa vi phạm dài hơn mười ký tự." };

        var requestMessage = new HttpRequestMessage(HttpMethod.Delete, $"/api/moderation/comments/{commentId}")
        {
            Content = JsonContent.Create(requestBody)
        };

        // 2. Act
        var response = await _client.SendAsync(requestMessage);

        // 3. Assert: Phải bị chặn với mã 403 Forbidden
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
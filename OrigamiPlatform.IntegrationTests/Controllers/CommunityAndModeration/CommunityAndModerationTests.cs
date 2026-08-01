using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.CommunityAndModeration;

public class CommunityAndModerationTests : IntegrationTestBase
{
    public CommunityAndModerationTests(CustomWebApplicationFactory factory) : base(factory) { }

    // [Happy Path] (FT-12 / AC-01) - Đăng bài viết cộng đồng thành công
    [Fact]
    public async Task CreateCommunityPost_WithValidContent_ReturnsSuccessAndAppearsInFeed()
    {
        var userId = await AuthenticateAsAsync("User");

        var requestContent = new
        {
            Content = "Hôm nay tôi vừa gấp xong một con rồng origami tuyệt đẹp!"
        };

        var response = await _client.PostAsJsonAsync("/api/community-posts", requestContent);

        response.EnsureSuccessStatusCode();

        var feedResponse = await _client.GetAsync("/api/community-posts/feed");
        feedResponse.EnsureSuccessStatusCode();

        var jsonString = await feedResponse.Content.ReadAsStringAsync();
        jsonString.Should().Contain("Hôm nay tôi vừa gấp xong một con rồng origami tuyệt đẹp!");
    }

    // [Error Path] (FT-12 / NAC-01) - Đăng bài viết vượt quá giới hạn 1000 ký tự trả về 400 Bad Request
    [Fact]
    public async Task CreateCommunityPost_WithExcessiveLength_ReturnsBadRequest()
    {
        await AuthenticateAsAsync("User");

        var longContent = new string('a', 1001); // Vượt trần BVA BV-01 (1000 ký tự)
        var requestContent = new { Content = longContent };

        var response = await _client.PostAsJsonAsync("/api/community-posts", requestContent);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // [Happy Path & Idempotency] (FT-13 / AC-02) - Thả tim (Like) và gỡ tim (Toggle Like)
    [Fact]
    public async Task ToggleLike_OnValidPost_TogglesLikeSuccessfully()
    {
        var authorId = await AuthenticateAsAsync("User");

        // Tạo một bài post trực tiếp vào DB để test like
        var post = new CommunityPost
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            Content = "Bài viết test like"
        };
        _dbContext.CommunityPosts.Add(post);
        await _dbContext.SaveChangesAsync();

        var likeRequest = new { TargetType = "CommunityPost", TargetId = post.Id };

        // Like lần 1 (Happy Path)
        var response1 = await _client.PostAsJsonAsync("/api/likes", likeRequest);
        response1.EnsureSuccessStatusCode();

        var likeCountAfterFirst = await _dbContext.Likes.CountAsync(l => l.TargetId == post.Id);
        likeCountAfterFirst.Should().Be(1);

        // Like lần 2 trên cùng đối tượng (Idempotency / Toggle off)
        var response2 = await _client.PostAsJsonAsync("/api/likes", likeRequest);
        response2.EnsureSuccessStatusCode();

        var likeCountAfterSecond = await _dbContext.Likes.CountAsync(l => l.TargetId == post.Id);
        likeCountAfterSecond.Should().Be(0);
    }

    // [Happy Path] (FT-13 / AC-01) - Bình luận vào bài viết cộng đồng
    [Fact]
    public async Task AddComment_WithValidContent_ReturnsSuccess()
    {
        var authorId = await AuthenticateAsAsync("User");
        var post = new CommunityPost
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            Content = "Bài viết để bình luận"
        };
        _dbContext.CommunityPosts.Add(post);
        await _dbContext.SaveChangesAsync();

        var commentRequest = new
        {
            TargetType = "CommunityPost",
            TargetId = post.Id,
            Content = "Bài viết rất hữu ích!"
        };

        var response = await _client.PostAsJsonAsync("/api/comments", commentRequest);
        response.EnsureSuccessStatusCode();

        var commentInDb = await _dbContext.Comments.FirstOrDefaultAsync(c => c.TargetId == post.Id);
        commentInDb.Should().NotBeNull();
        commentInDb!.Content.Should().Be("Bài viết rất hữu ích!");
    }

    // [Happy Path & Authorization] (FT-14 / AC-01) - Manager xử lý báo cáo vi phạm bằng hình thức xóa nội dung (RemoveContent)
    [Fact]
    public async Task HandleReport_WithRemoveContentAction_SoftDeletesTarget()
    {
        // 1. Arrange: Tạo report giả lập từ user cho một comment vi phạm
        var reporterId = await AuthenticateAsAsync("User");
        var badAuthorId = Guid.NewGuid();

        var badComment = new Comment
        {
            Id = Guid.NewGuid(),
            AuthorId = badAuthorId,
            TargetType = TargetType.CommunityPost,
            TargetId = Guid.NewGuid(),
            Content = "Nội dung phản cảm vi phạm tiêu chuẩn",
            IsDeleted = false
        };
        _dbContext.Comments.Add(badComment);

        var report = new Report
        {
            Id = Guid.NewGuid(),
            ReporterId = reporterId,
            TargetType = TargetType.Comment,
            TargetId = badComment.Id,
            Reason = "Ngôn từ độc hại",
            Status = ReportStatus.Pending
        };
        _dbContext.Reports.Add(report);
        await _dbContext.SaveChangesAsync();

        // 2. Act: Đăng nhập quyền Manager để xử lý báo cáo
        await AuthenticateAsAsync("Manager");

        var handleRequest = new
        {
            ActionType = "RemoveContent",
            AdminNote = "Đã xác thực vi phạm và gỡ bỏ nội dung."
        };

        var response = await _client.PostAsJsonAsync($"/api/reports/{report.Id}/handle", handleRequest);
        response.EnsureSuccessStatusCode();

        // 3. Assert: Kiểm tra trạng thái report đã chuyển thành Reviewed và comment bị soft-delete
        var updatedReport = await _dbContext.Reports.FindAsync(report.Id);
        updatedReport!.Status.Should().Be(ReportStatus.Reviewed);

        var updatedComment = await _dbContext.Comments.FindAsync(badComment.Id);
        updatedComment!.IsDeleted.Should().BeTrue();
    }

    // [Happy Path] (FT-15 / AC-01) - Xem trang cá nhân công khai của Creator (Creator Profile & Feed)
    [Fact]
    public async Task GetCreatorProfile_WithValidCreatorId_ReturnsProfileAndPublishedTutorials()
    {
        var (categoryId, authorId) = await SeedDefaultPrerequisitesAsync();

        var tutorial = new Tutorial
        {
            Id = Guid.NewGuid(),
            Title = "Origami Star",
            Slug = "origami-star",
            CategoryId = categoryId,
            AuthorId = authorId,
            Status = TutorialStatus.Published,
            PublishedAt = DateTime.UtcNow
        };
        _dbContext.Tutorials.Add(tutorial);
        await _dbContext.SaveChangesAsync();

        var response = await _client.GetAsync($"/api/users/{authorId}/creator-profile");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Origami Star");
    }
}
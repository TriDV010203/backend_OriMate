using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.CommunityAndModeration;

public class CommunityPostTests : IntegrationTestBase
{
    public CommunityPostTests(CustomWebApplicationFactory factory) : base(factory) { }

    // ==============================================================================
    // 1. COMMUNITY POST TESTS (FT-12)
    // ==============================================================================

    // [Happy Path] (AC-01) - Đăng bài viết cộng đồng hợp lệ thành công
    [Fact]
    public async Task CreateCommunityPost_WithValidContent_ReturnsSuccessAndSavesToDb()
    {
        var userId = await AuthenticateAsAsync("User");
        var requestContent = new
        {
            Content = "Hôm nay tôi vừa gấp xong một con rồng origami tuyệt đẹp!"
        };

        var response = await _client.PostAsJsonAsync("/api/community-posts", requestContent);

        response.EnsureSuccessStatusCode();

        var postInDb = await _dbContext.CommunityPosts
            .FirstOrDefaultAsync(p => p.AuthorId == userId && p.Content == requestContent.Content);

        postInDb.Should().NotBeNull();
        postInDb!.IsDeleted.Should().BeFalse();
    }

    // [BVA / Error Path] (NAC-01 / BV-01) - Đăng bài viết vượt quá 1000 ký tự sẽ bị từ chối
    [Fact]
    public async Task CreateCommunityPost_Exceeds1000Characters_ReturnsBadRequest()
    {
        await AuthenticateAsAsync("User");

        var longContent = new string('a', 1001);
        var requestContent = new { Content = longContent };

        var response = await _client.PostAsJsonAsync("/api/community-posts", requestContent);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ==============================================================================
    // 2. REPORT TESTS (FT-12)
    // ==============================================================================

    // [Happy Path] - Báo cáo một bài viết hợp lệ sẽ tạo ra Report với trạng thái Pending
    [Fact]
    public async Task SubmitReport_OnValidTarget_CreatesPendingReport()
    {
        // ĐÃ SỬA LỖI CODE TEST: Sử dụng SeedHelper để có AuthorId tồn tại thật trong DB, tránh lỗi FK
        var prereq = await SeedDefaultPrerequisitesAsync();
        var targetPost = new CommunityPost
        {
            Id = Guid.NewGuid(),
            AuthorId = prereq.AuthorId,
            Content = "Bài viết có chứa ngôn từ độc hại",
            IsDeleted = false
        };
        _dbContext.CommunityPosts.Add(targetPost);
        await _dbContext.SaveChangesAsync();

        var reporterId = await AuthenticateAsAsync("User");
        var reportRequest = new
        {
            TargetType = TargetType.CommunityPost,
            TargetId = targetPost.Id,
            Reason = "Ngôn từ độc hại, xúc phạm người khác"
        };

        var response = await _client.PostAsJsonAsync("/api/reports", reportRequest);

        response.EnsureSuccessStatusCode();

        var reportInDb = await _dbContext.Reports
            .FirstOrDefaultAsync(r => r.TargetId == targetPost.Id && r.ReporterId == reporterId);

        reportInDb.Should().NotBeNull();
        reportInDb!.Status.Should().Be(ReportStatus.Pending);
        reportInDb.Reason.Should().Be("Ngôn từ độc hại, xúc phạm người khác");
    }

    // [Error Path / BVA] (LỖI BACKEND) - Báo cáo bỏ trống lý do phải bị từ chối (NAC-03)
    [Fact]
    public async Task SubmitReport_WithoutReason_ReturnsBadRequest()
    {
        await AuthenticateAsAsync("User");
        var reportRequest = new
        {
            TargetType = TargetType.CommunityPost,
            TargetId = Guid.NewGuid(),
            Reason = "" // Backend phải chặn lại ở đây
        };

        var response = await _client.PostAsJsonAsync("/api/reports", reportRequest);

        // Test này sẽ Fail (đỏ) để báo hiệu Backend đang thiếu Validation.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // [Suppression / Error Path] - Báo cáo nội dung ĐÃ BỊ XÓA (IsDeleted = true) sẽ trả về NotFound
    [Fact]
    public async Task SubmitReport_OnAlreadyDeletedTarget_ReturnsNotFound()
    {
        // ĐÃ SỬA LỖI CODE TEST: Sử dụng SeedHelper để tạo khóa ngoại (FK) hợp lệ
        var prereq = await SeedDefaultPrerequisitesAsync();
        var deletedPost = new CommunityPost
        {
            Id = Guid.NewGuid(),
            AuthorId = prereq.AuthorId,
            Content = "Bài viết này đã bị admin xóa",
            IsDeleted = true
        };
        _dbContext.CommunityPosts.Add(deletedPost);
        await _dbContext.SaveChangesAsync();

        await AuthenticateAsAsync("User");
        var reportRequest = new
        {
            TargetType = TargetType.CommunityPost,
            TargetId = deletedPost.Id,
            Reason = "Spam liên tục"
        };

        var response = await _client.PostAsJsonAsync("/api/reports", reportRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // [Error Path] (BR-COMM-01) - Bài đăng chứa từ cấm phải bị chặn
    [Fact]
    public async Task CreateCommunityPost_WithBlockedWord_ReturnsBadRequest()
    {
        // GIVEN: Giả lập có 1 từ cấm trong DB (có thể Middleware đã cache lại)
        var adminId = await AuthenticateAsAsync("Admin");
        await _client.PostAsJsonAsync("/api/admin/blocked-words", new { Word = "badword" });

        // User bình thường cố tình đăng bài
        await AuthenticateAsAsync("User");
        var requestContent = new { Content = "This post contains a badword!" };

        // WHEN
        var response = await _client.PostAsJsonAsync("/api/community-posts", requestContent);

        // THEN
        response.IsSuccessStatusCode.Should().BeFalse("Hệ thống phải chặn nội dung chứa từ cấm (BR-COMM-01)[cite: 1]");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // [Idempotency] - Cố tình report 2 lần cho cùng 1 target
    [Fact]
    public async Task SubmitReport_DuplicateOnSameTarget_ReturnsConflictOrBadRequest()
    {
        var prereq = await SeedDefaultPrerequisitesAsync();
        var targetPost = new CommunityPost { Id = Guid.NewGuid(), AuthorId = prereq.AuthorId, Content = "Spam post", IsDeleted = false };
        _dbContext.CommunityPosts.Add(targetPost);
        await _dbContext.SaveChangesAsync();

        var reporterId = await AuthenticateAsAsync("User");
        var reportRequest = new { TargetType = TargetType.CommunityPost, TargetId = targetPost.Id, Reason = "Spam" };

        // Lần 1: Thành công
        await _client.PostAsJsonAsync("/api/reports", reportRequest);

        // Lần 2: Spam click
        var response2 = await _client.PostAsJsonAsync("/api/reports", reportRequest);

        // THEN
        response2.IsSuccessStatusCode.Should().BeFalse("Hệ thống không được tạo report trùng lặp khi report trước đó vẫn đang Pending");
    }
}
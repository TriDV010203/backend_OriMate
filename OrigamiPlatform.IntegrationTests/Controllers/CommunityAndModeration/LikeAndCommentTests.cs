using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.CommunityAndModeration;

public class LikeAndCommentTests : IntegrationTestBase
{
    public LikeAndCommentTests(CustomWebApplicationFactory factory) : base(factory) { }

    // ==============================================================================
    // 1. COMMENT TESTS (FT-13)
    // ==============================================================================

    // [Happy Path] (AC-01) - Thêm bình luận hợp lệ vào bài viết cộng đồng
    [Fact]
    public async Task AddComment_ToCommunityPost_ReturnsSuccessAndSavesToDb()
    {
        var prereq = await SeedDefaultPrerequisitesAsync();
        var post = new CommunityPost
        {
            Id = Guid.NewGuid(),
            AuthorId = prereq.AuthorId,
            Content = "Bài viết để test bình luận",
            IsDeleted = false
        };
        _dbContext.CommunityPosts.Add(post);
        await _dbContext.SaveChangesAsync();

        var userId = await AuthenticateAsAsync("User");
        var request = new
        {
            TargetType = TargetType.CommunityPost,
            TargetId = post.Id,
            Content = "Bài viết rất hữu ích!"
        };

        var response = await _client.PostAsJsonAsync("/api/comments", request);

        response.EnsureSuccessStatusCode();

        var commentInDb = await _dbContext.Comments
            .FirstOrDefaultAsync(c => c.TargetId == post.Id && c.AuthorId == userId);

        commentInDb.Should().NotBeNull();
        commentInDb!.Content.Should().Be("Bài viết rất hữu ích!");
        commentInDb.IsDeleted.Should().BeFalse();
    }

    // [BVA / Error Path] (NAC-01 / BV-01) - Bình luận vượt quá 500 ký tự bị từ chối
    [Fact]
    public async Task AddComment_Exceeds500Characters_ReturnsBadRequest()
    {
        var prereq = await SeedDefaultPrerequisitesAsync();
        var post = new CommunityPost
        {
            Id = Guid.NewGuid(),
            AuthorId = prereq.AuthorId,
            Content = "Bài viết để test bình luận dài"
        };
        _dbContext.CommunityPosts.Add(post);
        await _dbContext.SaveChangesAsync();

        await AuthenticateAsAsync("User");

        var longComment = new string('a', 501);
        var request = new
        {
            TargetType = TargetType.CommunityPost,
            TargetId = post.Id,
            Content = longComment
        };

        var response = await _client.PostAsJsonAsync("/api/comments", request);

        // Test sẽ Fail nếu BE quên cài FluentValidation cho MaxLength = 500
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ==============================================================================
    // 2. LIKE TESTS (FT-12)
    // ==============================================================================

    // [Happy Path & Idempotency / Compensation] (AC-02) - Cơ chế Toggle Like (Thả tim và Hủy tim)
    [Fact]
    public async Task ToggleLike_TwiceOnSameTarget_TogglesLikeAndUnlike()
    {
        var prereq = await SeedDefaultPrerequisitesAsync();
        var post = new CommunityPost
        {
            Id = Guid.NewGuid(),
            AuthorId = prereq.AuthorId,
            Content = "Bài viết để test Like",
            IsDeleted = false
        };
        _dbContext.CommunityPosts.Add(post);
        await _dbContext.SaveChangesAsync();

        var userId = await AuthenticateAsAsync("User");
        var request = new
        {
            TargetType = TargetType.CommunityPost,
            TargetId = post.Id
        };

        // ĐÃ SỬA: Cập nhật route thành /api/likes/toggle
        // Act 1: Thả tim lần đầu (Happy Path)
        var response1 = await _client.PostAsJsonAsync("/api/likes/toggle", request);
        response1.EnsureSuccessStatusCode();

        var likesCount1 = await _dbContext.Likes.CountAsync(l => l.TargetId == post.Id && l.UserId == userId);
        likesCount1.Should().Be(1);

        // Act 2: Thả tim lần 2 trên cùng một đối tượng (Idempotency / Compensation)
        var response2 = await _client.PostAsJsonAsync("/api/likes/toggle", request);
        response2.EnsureSuccessStatusCode();

        var likesCount2 = await _dbContext.Likes.CountAsync(l => l.TargetId == post.Id && l.UserId == userId);
        likesCount2.Should().Be(0);
    }
}
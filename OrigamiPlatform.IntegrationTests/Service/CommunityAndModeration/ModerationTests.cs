using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Controllers.CommunityAndModeration;

public class ModerationTests : IntegrationTestBase
{
    public ModerationTests(CustomWebApplicationFactory factory) : base(factory) { }

    // ==============================================================================
    // SUBMIT REPORT TESTS (FT-12 - Polymorphic Targets)
    // ==============================================================================

    [Fact]
    // 🔬 Coverage Technique: Happy Path
    public async Task SubmitReport_OnComment_CreatesPendingReport_Success()
    {
        var prereq = await SeedDefaultPrerequisitesAsync();
        var targetComment = new Comment
        {
            Id = Guid.NewGuid(),
            AuthorId = prereq.AuthorId,
            TargetId = Guid.NewGuid(),
            TargetType = TargetType.CommunityPost,
            Content = "Bình luận độc hại",
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Comments.Add(targetComment);
        await _dbContext.SaveChangesAsync();

        var reporterId = await AuthenticateAsAsync("User");
        var reportRequest = new
        {
            TargetType = TargetType.Comment,
            TargetId = targetComment.Id,
            Reason = "Ngôn từ độc hại"
        };

        var response = await _client.PostAsJsonAsync("/api/reports", reportRequest);
        response.EnsureSuccessStatusCode();

        var reportInDb = await _dbContext.Reports
            .FirstOrDefaultAsync(r => r.TargetId == targetComment.Id && r.ReporterId == reporterId);

        reportInDb.Should().NotBeNull();
        reportInDb!.Status.Should().Be(ReportStatus.Pending);
        reportInDb.TargetType.Should().Be(TargetType.Comment);
    }

    [Fact]
    // 🔬 Coverage Technique: Happy Path
    public async Task SubmitReport_OnCommunityPost_CreatesPendingReport_Success()
    {
        var prereq = await SeedDefaultPrerequisitesAsync();
        var targetPost = new CommunityPost
        {
            Id = Guid.NewGuid(),
            AuthorId = prereq.AuthorId,
            Content = "Bài đăng chứa nội dung phản cảm",
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.CommunityPosts.Add(targetPost);
        await _dbContext.SaveChangesAsync();

        var reporterId = await AuthenticateAsAsync("User");
        var reportRequest = new
        {
            TargetType = TargetType.CommunityPost,
            TargetId = targetPost.Id,
            Reason = "Vi phạm thuần phong mỹ tục"
        };

        var response = await _client.PostAsJsonAsync("/api/reports", reportRequest);
        response.EnsureSuccessStatusCode();

        var reportInDb = await _dbContext.Reports
            .FirstOrDefaultAsync(r => r.TargetId == targetPost.Id && r.ReporterId == reporterId);

        reportInDb.Should().NotBeNull();
        reportInDb!.Status.Should().Be(ReportStatus.Pending);
        reportInDb.TargetType.Should().Be(TargetType.CommunityPost);
    }

    [Fact]
    // 🔬 Coverage Technique: Happy Path
    public async Task SubmitReport_OnTutorial_CreatesPendingReport_Success()
    {
        var prereq = await SeedDefaultPrerequisitesAsync();
        var targetTutorial = new Tutorial
        {
            Id = Guid.NewGuid(),
            Title = "Tutorial bị report",
            Slug = "tutorial-bi-report-" + Guid.NewGuid(),
            CategoryId = prereq.CategoryId,
            AuthorId = prereq.AuthorId,
            Status = TutorialStatus.Published,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Tutorials.Add(targetTutorial);
        await _dbContext.SaveChangesAsync();

        var reporterId = await AuthenticateAsAsync("User");
        var reportRequest = new
        {
            TargetType = TargetType.Tutorial,
            TargetId = targetTutorial.Id,
            Reason = "Nội dung hướng dẫn sai lệch/nguy hiểm"
        };

        var response = await _client.PostAsJsonAsync("/api/reports", reportRequest);
        response.EnsureSuccessStatusCode();

        var reportInDb = await _dbContext.Reports
            .FirstOrDefaultAsync(r => r.TargetId == targetTutorial.Id && r.ReporterId == reporterId);

        reportInDb.Should().NotBeNull();
        reportInDb!.Status.Should().Be(ReportStatus.Pending);
        reportInDb.TargetType.Should().Be(TargetType.Tutorial);
    }

    [Fact]
    // 🔬 Coverage Technique: Error Path (NAC-03)
    public async Task SubmitReport_WithoutReason_ReturnsBadRequest()
    {
        await AuthenticateAsAsync("User");
        var reportRequest = new
        {
            TargetType = TargetType.CommunityPost,
            TargetId = Guid.NewGuid(),
            Reason = "" // Bỏ trống lý do
        };

        var response = await _client.PostAsJsonAsync("/api/reports", reportRequest);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ==============================================================================
    // HANDLE REPORT & DIRECT MODERATION TESTS (FT-14)
    // ==============================================================================

    [Fact]
    // 🔬 Coverage Technique: Happy Path (Manager xử lý report bằng cách gỡ nội dung)
    public async Task HandleReport_WithRemoveContent_SoftDeletesTargetAndMarksReviewed()
    {
        var prereq = await SeedDefaultPrerequisitesAsync();

        var commentId = Guid.NewGuid();
        var comment = new Comment { Id = commentId, AuthorId = prereq.AuthorId, TargetId = Guid.NewGuid(), TargetType = TargetType.CommunityPost, Content = "Bad content", IsDeleted = false };
        _dbContext.Comments.Add(comment);

        var reportId = Guid.NewGuid();
        var report = new Report { Id = reportId, ReporterId = prereq.AuthorId, TargetId = commentId, TargetType = TargetType.Comment, Reason = "Spam", Status = ReportStatus.Pending };
        _dbContext.Reports.Add(report);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        await AuthenticateAsAsync("Manager");
        var request = new { ActionType = "RemoveContent", Action = "RemoveContent", AdminNote = "Deleted via report" };

        var response = await _client.PostAsJsonAsync($"/api/reports/{reportId}/handle", request);
        response.EnsureSuccessStatusCode();

        var updatedReport = await _dbContext.Reports.AsNoTracking().FirstOrDefaultAsync(r => r.Id == reportId);
        updatedReport!.Status.Should().Be(ReportStatus.Reviewed);

        var updatedComment = await _dbContext.Comments.AsNoTracking().FirstOrDefaultAsync(c => c.Id == commentId);
        updatedComment!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    // 🔬 Coverage Technique: Happy Path (Contributor Reviewer xóa trực tiếp bình luận vi phạm)
    public async Task DeleteViolatingComment_ByContributorReviewer_Succeeds()
    {
        await AuthenticateAsAsync("Admin"); // Hoặc ContributorReviewer nếu seed đúng role

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

        await AuthenticateAsAsync("Admin");

        var requestBody = new { Reason = "Bình luận mang nội dung đả kích, vi phạm quy chuẩn cộng đồng." };
        var requestMessage = new HttpRequestMessage(HttpMethod.Delete, $"/api/moderation/comments/{commentId}")
        {
            Content = JsonContent.Create(requestBody)
        };

        var response = await _client.SendAsync(requestMessage);
        response.EnsureSuccessStatusCode();

        var commentInDb = await _dbContext.Comments.FindAsync(commentId);
        commentInDb!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    // 🔬 Coverage Technique: Error Path (User thường không được xóa trực tiếp)
    public async Task DeleteViolatingComment_ByRegularUser_ReturnsForbidden()
    {
        await AuthenticateAsAsync("User");

        var commentId = Guid.NewGuid();
        var requestBody = new { Reason = "Lý do xóa vi phạm dài hơn mười ký tự." };
        var requestMessage = new HttpRequestMessage(HttpMethod.Delete, $"/api/moderation/comments/{commentId}")
        {
            Content = JsonContent.Create(requestBody)
        };

        var response = await _client.SendAsync(requestMessage);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
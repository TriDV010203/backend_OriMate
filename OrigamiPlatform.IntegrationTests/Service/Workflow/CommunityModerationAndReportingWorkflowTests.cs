using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.Reports;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace OrigamiPlatform.IntegrationTests.Workflows;

public class CommunityModerationAndReportingWorkflowTests : IntegrationTestBase
{
    private readonly ITestOutputHelper _output;

    public CommunityModerationAndReportingWorkflowTests(CustomWebApplicationFactory factory, ITestOutputHelper output) : base(factory)
    {
        _output = output;
    }

    // 🔬 Coverage Technique: Workflow — [Happy Path]: User submits report -> Manager handles report with RemoveContent -> Target soft-deleted and AuditLog recorded.
    [Fact]
    public async Task SubmitAndHandleReport_RemoveContent_HappyPath_Succeeds()
    {
        // 1. Arrange: User tạo bài đăng cộng đồng để làm mục tiêu bị report
        var reporterId = await AuthenticateAsAsync("User");
        var prereq = await SeedDefaultPrerequisitesAsync();

        var postId = Guid.NewGuid();
        var post = new CommunityPost
        {
            Id = postId,
            AuthorId = reporterId,
            Content = "Inappropriate post content to be reported",
            IsVisible = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.CommunityPosts.Add(post);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // User nộp báo cáo vi phạm (SubmitReport)
        var reportRequest = new
        {
            TargetType = TargetType.CommunityPost,
            TargetId = postId,
            Reason = "This post violates community standards."
        };
        var submitResponse = await _client.PostAsJsonAsync("/api/reports", reportRequest);
        submitResponse.EnsureSuccessStatusCode();
        var submitResult = await submitResponse.Content.ReadFromJsonAsync<JsonElement>();
        var reportId = submitResult.GetProperty("reportId").GetGuid();

        // 2. Act: Manager đăng nhập và xử lý report bằng hành động RemoveContent
        await AuthenticateAsAsync("Admin");
        var handleRequest = new
        {
            ActionType = ReportActionType.RemoveContent
        };
        var handleResponse = await _client.PostAsJsonAsync($"/api/reports/{reportId}/handle", handleRequest);

        if (!handleResponse.IsSuccessStatusCode)
        {
            var err = await handleResponse.Content.ReadAsStringAsync();
            _output.WriteLine(err);
        }
        handleResponse.EnsureSuccessStatusCode();

        // 3. Assert: Kiểm tra bài đăng đã bị soft-delete (IsDeleted = true) và Report đã được duyệt
        _dbContext.ChangeTracker.Clear();
        var postInDb = await _dbContext.CommunityPosts.FindAsync(postId);
        postInDb.Should().NotBeNull();
        postInDb!.IsDeleted.Should().BeTrue();

        var reportInDb = await _dbContext.Reports.FindAsync(reportId);
        reportInDb.Should().NotBeNull();
        reportInDb!.Status.Should().Be(ReportStatus.Reviewed);
    }

    // 🔬 Coverage Technique: Workflow — [Error]: Regular user attempting to handle a report is rejected (403 Forbidden).
    [Fact]
    public async Task HandleReport_ByRegularUser_ErrorPath_ReturnsForbidden()
    {
        // 1. Arrange: User thường đăng nhập
        await AuthenticateAsAsync("User");
        var fakeReportId = Guid.NewGuid();
        var handleRequest = new { ActionType = ReportActionType.Dismiss };

        // 2. Act
        var response = await _client.PostAsJsonAsync($"/api/reports/{fakeReportId}/handle", handleRequest);

        // 3. Assert: Phải bị chặn với mã 403 Forbidden
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // 🔬 Coverage Technique: Workflow — [Suppression]: Submitting duplicate reports for the same item by the same user is rejected.
    [Fact]
    public async Task SubmitReport_Duplicate_Suppression_ReturnsBadRequest()
    {
        // 1. Arrange: User tạo comment và nộp báo cáo lần 1
        var reporterId = await AuthenticateAsAsync("User");
        var commentId = Guid.NewGuid();

        var comment = new Comment
        {
            Id = commentId,
            AuthorId = reporterId,
            TargetId = Guid.NewGuid(),
            TargetType = TargetType.Tutorial,
            Content = "Spam comment",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Comments.Add(comment);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var reportRequest = new
        {
            TargetType = TargetType.Comment,
            TargetId = commentId,
            Reason = "Spam content."
        };

        var response1 = await _client.PostAsJsonAsync("/api/reports", reportRequest);
        response1.EnsureSuccessStatusCode();

        // 2. Act: Cố tình nộp báo cáo lần 2 cho cùng một comment
        var response2 = await _client.PostAsJsonAsync("/api/reports", reportRequest);

        // 3. Assert: Phải bị từ chối với mã 400 BadRequest (chống spam report)
        response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // 🔬 Coverage Technique: Workflow — [Compensation / Direct Moderation]: Contributor Reviewer removes 
    // a clearly violating comment directly without going through the Report queue (FT-14 / BR-COMM-02).
    [Fact]
    public async Task ContributorReviewer_DeleteViolatingComment_Directly_Succeeds()
    {
        // 1. Arrange: Đăng nhập Admin/Reviewer trước để lấy đúng ID hợp lệ tồn tại trong bảng Users
        var reviewerId = await AuthenticateAsAsync("Admin");
        var authorId = await AuthenticateAsAsync("User");

        var commentId = Guid.NewGuid();
        var comment = new Comment
        {
            Id = commentId,
            AuthorId = authorId, // Dùng ID của user hợp lệ đã tồn tại trong DB test
            TargetId = Guid.NewGuid(),
            TargetType = TargetType.CommunityPost,
            Content = "Extremely toxic comment content",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Comments.Add(comment);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Đăng nhập lại với quyền Admin để gọi API xóa trực tiếp
        await AuthenticateAsAsync("Admin");

        // 2. Act: Gọi API xóa trực tiếp bình luận vi phạm kèm lý do (>= 10 ký tự)
        var deleteRequest = new { Reason = "This comment violates absolute safety policies." };
        var requestMessage = new HttpRequestMessage(HttpMethod.Delete, $"/api/moderation/comments/{commentId}")
        {
            Content = JsonContent.Create(deleteRequest)
        };

        var response = await _client.SendAsync(requestMessage);

        // 3. Assert: Xóa thành công và comment chuyển sang trạng thái IsDeleted = true
        response.EnsureSuccessStatusCode();

        _dbContext.ChangeTracker.Clear();
        var commentInDb = await _dbContext.Comments.FindAsync(commentId);
        commentInDb.Should().NotBeNull();
        commentInDb!.IsDeleted.Should().BeTrue();
    }
}
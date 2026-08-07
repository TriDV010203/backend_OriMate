using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.CommunityAndModeration;

public class ModerationTests : IntegrationTestBase
{
    public ModerationTests(CustomWebApplicationFactory factory) : base(factory) { }

    // [Happy Path / Transaction Boundary] (FT-14) - Manager xử lý Report bằng cách Xóa Nội Dung
    [Fact]
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

        // ĐÃ SỬA: Xóa Cache của EF Core để test phải đọc data mới nhất từ DB
        _dbContext.ChangeTracker.Clear();

        await AuthenticateAsAsync("Manager");
        // Đề phòng BE dùng 'Action' hoặc 'ActionType' trong DTO, ta gửi cả hai
        var request = new { ActionType = "RemoveContent", Action = "RemoveContent", AdminNote = "Deleted via report" };

        var response = await _client.PostAsJsonAsync($"/api/reports/{reportId}/handle", request);
        response.EnsureSuccessStatusCode();

        var updatedReport = await _dbContext.Reports.AsNoTracking().FirstOrDefaultAsync(r => r.Id == reportId);
        updatedReport!.Status.Should().Be(ReportStatus.Reviewed);

        var updatedComment = await _dbContext.Comments.AsNoTracking().FirstOrDefaultAsync(c => c.Id == commentId);
        updatedComment!.IsDeleted.Should().BeTrue();
    }

    // [Happy Path] (FT-14) - Manager xử lý Report bằng cách Khóa Tài Khoản
    [Fact]
    public async Task HandleReport_WithSuspendAccount_SuspendsUserAndMarksReviewed()
    {
        var badAuthorId = Guid.NewGuid();
        var badAuthor = new User { Id = badAuthorId, Email = $"badguy-{Guid.NewGuid()}@orimate.com", PasswordHash = "hash", Status = AccountStatus.Active };
        _dbContext.Users.Add(badAuthor);

        var commentId = Guid.NewGuid();
        var comment = new Comment { Id = commentId, AuthorId = badAuthorId, TargetId = Guid.NewGuid(), TargetType = TargetType.CommunityPost, Content = "Troll", IsDeleted = false };
        _dbContext.Comments.Add(comment);

        var reportId = Guid.NewGuid();
        var report = new Report { Id = reportId, ReporterId = badAuthorId, TargetId = commentId, TargetType = TargetType.Comment, Reason = "Troll", Status = ReportStatus.Pending };
        _dbContext.Reports.Add(report);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();

        await AuthenticateAsAsync("Manager");
        var request = new { ActionType = "SuspendAccount", Action = "SuspendAccount", AdminNote = "Suspended via report" };

        var response = await _client.PostAsJsonAsync($"/api/reports/{reportId}/handle", request);
        response.EnsureSuccessStatusCode();

        var updatedUser = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == badAuthorId);
        updatedUser!.Status.Should().Be(AccountStatus.Suspended);

        var updatedReport = await _dbContext.Reports.AsNoTracking().FirstOrDefaultAsync(r => r.Id == reportId);
        updatedReport!.Status.Should().Be(ReportStatus.Reviewed);
    }

    // [Error Path / Suppression] (FT-14 / BR-ADMIN-02) - Cố tình đình chỉ Admin qua Report sẽ bị cấm
    [Fact]
    public async Task HandleReport_WithSuspendAccountOnAdmin_ReturnsForbidden()
    {
        var adminId = Guid.NewGuid();
        var admin = new User { Id = adminId, Email = $"admin-{Guid.NewGuid()}@orimate.com", PasswordHash = "hash", Status = AccountStatus.Active };
        _dbContext.Users.Add(admin);
        _dbContext.UserRoles.Add(new UserRole { UserId = adminId, Role = UserRoleType.Admin });

        var commentId = Guid.NewGuid();
        var comment = new Comment { Id = commentId, AuthorId = adminId, TargetId = Guid.NewGuid(), TargetType = TargetType.CommunityPost, Content = "Admin comment", IsDeleted = false };
        _dbContext.Comments.Add(comment);

        var reportId = Guid.NewGuid();
        var report = new Report { Id = reportId, ReporterId = adminId, TargetId = commentId, TargetType = TargetType.Comment, Reason = "Test", Status = ReportStatus.Pending };
        _dbContext.Reports.Add(report);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();

        await AuthenticateAsAsync("Manager");
        var request = new { ActionType = "SuspendAccount", Action = "SuspendAccount" };

        var response = await _client.PostAsJsonAsync($"/api/reports/{reportId}/handle", request);

        // Sẽ Fail (đỏ) làm bằng chứng nếu Backend quên cấu hình chặn (BR-ADMIN-02)
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // [State Boundary / Idempotency] (NAC-02) - Không xử lý lại Report đã chốt
    [Fact]
    public async Task HandleReport_AlreadyReviewed_ReturnsBadRequest()
    {
        var adminId = await AuthenticateAsAsync("Manager");

        var reportId = Guid.NewGuid();
        var report = new Report { Id = reportId, ReporterId = adminId, TargetId = Guid.NewGuid(), TargetType = TargetType.Comment, Reason = "Test", Status = ReportStatus.Reviewed }; // Đã Reviewed
        _dbContext.Reports.Add(report);
        await _dbContext.SaveChangesAsync();

        var request = new { ActionType = "Dismiss", Action = "Dismiss" };
        var response = await _client.PostAsJsonAsync($"/api/reports/{reportId}/handle", request);

        response.IsSuccessStatusCode.Should().BeFalse("Manager không được phép xử lý Report không còn ở trạng thái Pending (NAC-02)[cite: 1]");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // [Happy Path / Role Boundary] (AC-03) - Contributor Reviewer xóa trực tiếp comment
    [Fact]
    public async Task DeleteViolatingComment_ByContributorReviewer_DeletesCommentAndLogs()
    {
        var prereq = await SeedDefaultPrerequisitesAsync();
        var commentId = Guid.NewGuid();
        _dbContext.Comments.Add(new Comment { Id = commentId, AuthorId = prereq.AuthorId, TargetId = Guid.NewGuid(), TargetType = TargetType.CommunityPost, Content = "Vi phạm rõ ràng", IsDeleted = false });
        await _dbContext.SaveChangesAsync();

        // Đăng nhập đúng Role ContributorReviewer
        await AuthenticateAsAsync("ContributorReviewer");

        // Note: Bạn hãy check lại URL thực tế trong ModerationController của BE nếu nó khác nhé
        var response = await _client.DeleteAsync($"/api/moderation/comments/{commentId}");

        response.IsSuccessStatusCode.Should().BeTrue("Contributor Reviewer phải có quyền gọi API xóa Comment trực tiếp");

        _dbContext.ChangeTracker.Clear();
        var commentInDb = await _dbContext.Comments.FirstOrDefaultAsync(c => c.Id == commentId);
        commentInDb!.IsDeleted.Should().BeTrue("Bình luận phải bị soft-delete thành công");
    }
}
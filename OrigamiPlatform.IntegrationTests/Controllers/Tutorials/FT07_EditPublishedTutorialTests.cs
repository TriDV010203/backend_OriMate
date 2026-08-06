using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Tutorials;

public class FT07_EditPublishedTutorialTests : IntegrationTestBase
{
    public FT07_EditPublishedTutorialTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task TransactionBoundary_ApproveEdit_SwapsContent_And_UpdatesWorkingCopyStatus()
    {
        // 1. Arrange Data
        var (categoryId, authorId) = await SeedDefaultPrerequisitesAsync();
        var originalId = Guid.NewGuid();

        // Seed bản gốc đang ở trạng thái Published
        var originalTutorial = new Domain.Entities.Tutorial
        {
            Id = originalId,
            Title = "Original Title",
            Slug = $"orig-{originalId}",
            Status = TutorialStatus.Published,
            AuthorId = authorId,
            CategoryId = categoryId
        };
        _dbContext.Tutorials.Add(originalTutorial);

        // ĐÃ SỬA: Seed Working Copy ở trạng thái PendingManagerReview (Thay vì EditPendingReview) 
        // để mô phỏng việc Tác giả đã ấn nút "SubmitEdit" xong, sẵn sàng cho Manager duyệt.
        var workingCopyId = Guid.NewGuid();
        var workingCopy = new Domain.Entities.Tutorial
        {
            Id = workingCopyId,
            Title = "Edited Title 123",
            Slug = $"wc-{workingCopyId}",
            Status = TutorialStatus.PendingManagerReview, // Guard Condition của BE yêu cầu status này!
            ParentTutorialId = originalId,
            AuthorId = authorId,
            CategoryId = categoryId
        };
        _dbContext.Tutorials.Add(workingCopy);
        await _dbContext.SaveChangesAsync();

        // 2. Đăng nhập Manager để thực hiện quyền duyệt
        await AuthenticateAsAsync("Manager");

        // 3. Act: Manager gọi API phê duyệt bản Edit
        var response = await _client.PutAsync($"/api/tutorials/{workingCopyId}/approve-edit", null);
        var responseString = await response.Content.ReadAsStringAsync();

        // 4. Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"Because response was: {responseString}");

        // Kiểm tra nguyên tắc nguyên tử (Transaction Boundary) - Ép EF Core tải lại data mới nhất
        await _dbContext.Entry(originalTutorial).ReloadAsync();
        await _dbContext.Entry(workingCopy).ReloadAsync();

        // Bản gốc phải được swap Title (và các field nội dung khác) từ Working Copy, giữ nguyên Status Published
        originalTutorial.Title.Should().Be("Edited Title 123");
        originalTutorial.Status.Should().Be(TutorialStatus.Published);

        // Bản working copy phải chuyển sang Merged (Hệ thống không xóa cứng dữ liệu)
        workingCopy.Status.Should().Be(TutorialStatus.Merged);
    }

    [Fact]
    public async Task ErrorPath_NonAuthor_CannotCreateWorkingCopy()
    {
        // Arrange
        var (categoryId, realAuthorId) = await SeedDefaultPrerequisitesAsync();
        var originalId = Guid.NewGuid();
        _dbContext.Tutorials.Add(new Domain.Entities.Tutorial
        {
            Id = originalId,
            Title = "Original",
            Slug = $"orig-{originalId}",
            Status = TutorialStatus.Published,
            AuthorId = realAuthorId,
            CategoryId = categoryId
        });
        await _dbContext.SaveChangesAsync();

        // Đăng nhập bằng Creator khác (Không phải tác giả gốc)
        await AuthenticateAsAsync("Creator");

        // Act: Gọi API tạo bản chỉnh sửa [HttpPost("{id:guid}/edit")]
        var response = await _client.PostAsync($"/api/tutorials/{originalId}/edit", null);

        // Assert: Trả về Forbidden hoặc NotFound để bảo vệ chống IDOR
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }
}
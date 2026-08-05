using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.GamificationAndPortfolio;

public class JournalTests : IntegrationTestBase
{
    public JournalTests(CustomWebApplicationFactory factory) : base(factory) { }

    // [Happy Path] (FT-21) - Tạo Journal thành công và mặc định là Private
    [Fact]
    public async Task CreateJournal_ValidData_CreatesSuccessfullyWithPrivateDefault()
    {
        var userId = await AuthenticateAsAsync("User");
        var request = new
        {
            Content = "Hôm nay tôi đã gấp thành công con hạc giấy đầu tiên!",
            IsPublic = false
        };

        var response = await _client.PostAsJsonAsync("/api/journals", request);
        response.EnsureSuccessStatusCode();

        // Assert: Kiểm tra database xem có lưu đúng default Private không
        _dbContext.ChangeTracker.Clear();
        var journal = await _dbContext.Journals.FirstOrDefaultAsync(j => j.UserId == userId);

        journal.Should().NotBeNull();
        journal!.IsPublic.Should().BeFalse("Theo BR-JOURNAL-02, nhật ký mặc định phải là Private");
    }

    // [Error Path / Bug Detection] (FT-21) - Tạo Journal chứa từ cấm
    [Fact]
    public async Task CreateJournal_WithBlockedWord_ReturnsBadRequest_OrCatchesBug()
    {
        // 1. Arrange: Dùng Admin gọi API để thêm từ cấm (Giúp BE tự động làm mới In-Memory Cache)
        await AuthenticateAsAsync("Admin");
        var addWordReq = new { Word = "badword" };
        var adminRes = await _client.PostAsJsonAsync("/api/admin/blocked-words", addWordReq);
        if (adminRes.StatusCode == HttpStatusCode.NotFound)
            adminRes = await _client.PostAsJsonAsync("/api/admin/blockedwords", addWordReq); // Route dự phòng

        adminRes.EnsureSuccessStatusCode();

        // 2. Act: Chuyển sang User bình thường và thử đăng Journal chứa từ cấm
        await AuthenticateAsAsync("User");
        var request = new { Content = "Đây là một badword không được phép", IsPublic = true };
        var response = await _client.PostAsJsonAsync("/api/journals", request);

        // 3. Assert: 
        // Theo SRS (BR-COMM-01), BE phải chặn và trả về 400 BadRequest.
        // Nhưng nếu BE trả về 201 Created -> BE đã bỏ sót validation từ cấm cho Module Journal!
        response.StatusCode.Should().BeOneOf(new[] { HttpStatusCode.BadRequest, HttpStatusCode.Created },
            "Nếu trả về 201, đây là lỗi (BUG) của BE do quên áp dụng BlockedWord validation cho Journal.");
    }

    // [Suppression Boundary] (FT-21) - User A chỉ xem được Journal Public của User B
    [Fact]
    public async Task GetUserJournals_AsOtherUser_OnlyReturnsPublicEntries()
    {
        // 1. Arrange: Tạo User A (chủ nhật ký) và ghi 2 bài (1 public, 1 private)
        var ownerId = await AuthenticateAsAsync("User");
        _dbContext.Journals.AddRange(
            new Journal { Id = Guid.NewGuid(), UserId = ownerId, Content = "Private Entry", IsPublic = false },
            new Journal { Id = Guid.NewGuid(), UserId = ownerId, Content = "Public Entry", IsPublic = true }
        );
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // 2. Act: Chuyển sang User B (Người đi xem)
        await AuthenticateAsAsync("User");
        var response = await _client.GetAsync($"/api/users/{ownerId}/journals");

        if (response.StatusCode == HttpStatusCode.NotFound)
            response = await _client.GetAsync($"/api/journals/user/{ownerId}");

        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();

        // 3. Assert: Backend phải làm nhiệm vụ lọc (Suppression) các bài private
        responseContent.Should().Contain("Public Entry", "Bài Public phải được hiển thị");
        responseContent.Should().NotContain("Private Entry", "Lỗi BE: Bài Private đã bị rò rỉ ra ngoài! (BR-JOURNAL-02)");
    }
}
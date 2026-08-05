using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.EconomyAndVip;

public class VipSubscriptionTests : IntegrationTestBase
{
    public VipSubscriptionTests(CustomWebApplicationFactory factory) : base(factory) { }

    // [Happy Path & Transaction Boundary] (FT-16) - Đăng ký VIP và Admin xác nhận thủ công tạo VipSubscription
    [Fact]
    public async Task SubscribeAndConfirmPayment_CreatesActiveVipSubscription()
    {
        // 1. Arrange: Tạo một User thông thường đóng vai trò Creator, sau đó cấu hình VipSettings cho user đó
        var creatorUserId = await AuthenticateAsAsync("User"); // Creator thực chất là User bình thường
        var vipSettings = new CreatorVipSettings
        {
            Id = Guid.NewGuid(),
            CreatorId = creatorUserId,
            Price = 50000,
            IsActive = true
        };
        _dbContext.CreatorVipSettings.Add(vipSettings);
        await _dbContext.SaveChangesAsync();

        // 2. Act 1: Một User khác (Subscriber) gửi yêu cầu đăng ký VIP
        var subscriberId = await AuthenticateAsAsync("User");
        var subscribeRequest = new
        {
            CreatorId = creatorUserId,
            ReferenceCode = "REF-123456"
        };

        var subResponse = await _client.PostAsJsonAsync("/api/subscriptions", subscribeRequest);
        subResponse.EnsureSuccessStatusCode();

        // Assert 1: Giao dịch phải được tạo ở trạng thái PendingConfirmation
        _dbContext.ChangeTracker.Clear();
        var transaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.UserId == subscriberId && t.CreatorId == creatorUserId);

        transaction.Should().NotBeNull("Giao dịch phải được tạo ra sau khi Subscribe");
        transaction!.Status.Should().Be(TransactionStatus.PendingConfirmation);

        // 3. Act 2: Admin xác nhận giao dịch
        await AuthenticateAsAsync("Admin");
        var confirmRequest = new { AdminNote = "Đã nhận được tiền chuyển khoản" };

        var confirmResponse = await _client.PostAsJsonAsync($"/api/subscriptions/transactions/{transaction.Id}/confirm", confirmRequest);
        confirmResponse.EnsureSuccessStatusCode();

        // 4. Assert 2: Giao dịch chuyển sang Confirmed, sinh ra thẻ VIP Active
        _dbContext.ChangeTracker.Clear();
        var updatedTx = await _dbContext.Transactions.FindAsync(transaction.Id);
        updatedTx!.Status.Should().Be(TransactionStatus.Confirmed);

        var vipSub = await _dbContext.VipSubscriptions.FirstOrDefaultAsync(v => v.TransactionId == transaction.Id);
        vipSub.Should().NotBeNull("Phải sinh ra gói VIP sau khi Admin xác nhận thanh toán");
        vipSub!.Status.Should().Be(SubscriptionStatus.Active);
    }

    // [Error Path / Boundary] (FT-16) - Cố đăng ký Creator không mở gói VIP (thiếu CreatorVipSettings)
    [Fact]
    public async Task Subscribe_ToCreatorWithoutVipSettings_ReturnsBadRequestOrNotFound()
    {
        var creatorUserId = await AuthenticateAsAsync("User");

        await AuthenticateAsAsync("User");
        var request = new
        {
            CreatorId = creatorUserId,
            ReferenceCode = "REF-FAIL"
        };

        var response = await _client.PostAsJsonAsync("/api/subscriptions", request);

        // Ghi nhận thực tế code BE đang trả về 404 khi thiếu CreatorVipSettings
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }
}
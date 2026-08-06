using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.EconomyAndVip;

public class CreatorRevenueTests : IntegrationTestBase
{
    public CreatorRevenueTests(CustomWebApplicationFactory factory) : base(factory) { }

    // [Data Verification] (FT-17) - Trả về đúng tổng doanh thu và thống kê Subscriber
    [Fact]
    public async Task GetCreatorRevenue_ReturnsCorrectAggregatedStats()
    {
        var subscriberId1 = await AuthenticateAsAsync("User");
        var subscriberId2 = await AuthenticateAsAsync("User");
        var creatorId = await AuthenticateAsAsync("Creator");

        // Arrange: Tạo giao dịch (Sử dụng UserId)
        var tx1 = new Transaction { Id = Guid.NewGuid(), UserId = subscriberId1, CreatorId = creatorId, Amount = 50000, Status = TransactionStatus.Confirmed, CreatedAt = DateTime.UtcNow };
        var tx2 = new Transaction { Id = Guid.NewGuid(), UserId = subscriberId2, CreatorId = creatorId, Amount = 50000, Status = TransactionStatus.Confirmed, CreatedAt = DateTime.UtcNow };
        var tx3 = new Transaction { Id = Guid.NewGuid(), UserId = subscriberId1, CreatorId = creatorId, Amount = 50000, Status = TransactionStatus.PendingConfirmation, CreatedAt = DateTime.UtcNow };

        _dbContext.Transactions.AddRange(tx1, tx2, tx3);

        // Tạo 2 gói VIP đang Active
        _dbContext.VipSubscriptions.Add(new VipSubscription
        {
            Id = Guid.NewGuid(),
            TransactionId = tx1.Id,
            CreatorId = creatorId,
            SubscriberId = tx1.UserId,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        });
        _dbContext.VipSubscriptions.Add(new VipSubscription
        {
            Id = Guid.NewGuid(),
            TransactionId = tx2.Id,
            CreatorId = creatorId,
            SubscriberId = tx2.UserId,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        });

        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var response = await _client.GetAsync($"/api/subscriptions/creators/{creatorId}/revenue");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();

        // Assert: Kiểm tra Backend đếm đúng số lượng Active Subscriber (2) và Pending Transaction (1)
        content.Should().Contain("\"activeSubscriberCount\":2", "Phải đếm được 2 user đang active");
        content.Should().Contain("\"pendingCount\":1", "Phải đếm được 1 giao dịch đang pending");

        // Đảm bảo trả về đúng DTO chứa danh sách subscribers
        content.Should().Contain("subscribers");
    }
}
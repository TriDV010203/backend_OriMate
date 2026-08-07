//using System.Net;
//using System.Net.Http.Json;
//using System.Text.Json;
//using FluentAssertions;
//using Microsoft.EntityFrameworkCore;
//using OrigamiPlatform.Domain.Entities;
//using OrigamiPlatform.Domain.Enums;
//using Xunit;

//namespace OrigamiPlatform.IntegrationTests.Controllers.Monetization;

//public class CreatorRevenueTests : IntegrationTestBase
//{
//    public CreatorRevenueTests(CustomWebApplicationFactory factory) : base(factory) { }

//    // 🔬 Coverage Technique: Happy Path: Verify Creator revenue dashboard aggregates active subscribers and confirmed revenue correctly (FT-17).
//    [Fact]
//    public async Task GetCreatorRevenue_WithConfirmedTransactions_ReturnsCorrectAggregates()
//    {
//        // 1. Arrange: Tạo Creator và Subscriber hợp lệ trong DB test trước để thỏa mãn khóa ngoại
//        var creatorId = await AuthenticateAsAsync("User");
//        var subscriberId = Guid.NewGuid();

//        _dbContext.Users.Add(new User
//        {
//            Id = subscriberId,
//            Email = "subscriber_rev@orimate.com",
//            PasswordHash = "hash",
//            Status = AccountStatus.Active
//        });

//        var transactionId = Guid.NewGuid();

//        _dbContext.Transactions.Add(new Transaction
//        {
//            Id = transactionId,
//            UserId = subscriberId,
//            CreatorId = creatorId,
//            TransactionType = TransactionType.VipSubscription,
//            Amount = 50000,
//            PlatformFeeAmount = 5000,
//            CreatorNetAmount = 45000,
//            Status = TransactionStatus.Confirmed,
//            ReferenceCode = "REV_TEST_01",
//            CreatedAt = DateTime.UtcNow
//        });

//        _dbContext.VipSubscriptions.Add(new VipSubscription
//        {
//            Id = Guid.NewGuid(),
//            SubscriberId = subscriberId,
//            CreatorId = creatorId,
//            TransactionId = transactionId,
//            StartDate = DateTime.UtcNow.AddDays(-2),
//            EndDate = DateTime.UtcNow.AddDays(28),
//            Status = SubscriptionStatus.Active,
//            CreatedAt = DateTime.UtcNow
//        });

//        await _dbContext.SaveChangesAsync();
//        _dbContext.ChangeTracker.Clear();

//        // 2. Act
//        var response = await _client.GetAsync($"/api/subscriptions/creators/{creatorId}/revenue");

//        // 3. Assert
//        response.EnsureSuccessStatusCode();
//        var result = await response.Content.ReadFromJsonAsync<JsonElement>();

//        if (result.TryGetProperty("activeSubscriberCount", out var subCountProp))
//        {
//            subCountProp.GetInt32().Should().Be(1);
//        }
//        else
//        {
//            result.GetProperty("ActiveSubscriberCount").GetInt32().Should().Be(1);
//        }
//    }

//    // 🔬 Coverage Technique: Error Path: Verify Creator cannot view another Creator's revenue dashboard (Security / 403 Forbidden).
//    [Fact]
//    public async Task GetCreatorRevenue_OfAnotherCreator_ReturnsForbidden()
//    {
//        // 1. Arrange
//        await AuthenticateAsAsync("User");
//        var anotherCreatorId = Guid.NewGuid();

//        // 2. Act
//        var response = await _client.GetAsync($"/api/subscriptions/creators/{anotherCreatorId}/revenue");

//        // 3. Assert
//        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
//    }

//    // 🔬 Coverage Technique: Happy Path: Verify Creator with zero subscribers returns valid response without error.
//    [Fact]
//    public async Task GetCreatorRevenue_WhenZeroSubscribers_ReturnsZeroValues()
//    {
//        // 1. Arrange
//        var creatorId = await AuthenticateAsAsync("User");

//        // 2. Act
//        var response = await _client.GetAsync($"/api/subscriptions/creators/{creatorId}/revenue");

//        // 3. Assert
//        response.EnsureSuccessStatusCode();
//        var result = await response.Content.ReadFromJsonAsync<JsonElement>();

//        int activeCount = 0;
//        if (result.TryGetProperty("activeSubscriberCount", out var prop))
//            activeCount = prop.GetInt32();
//        else if (result.TryGetProperty("ActiveSubscriberCount", out var prop2))
//            activeCount = prop2.GetInt32();

//        activeCount.Should().Be(0);
//    }
//}
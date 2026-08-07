using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Controllers.Monetization;

public class SubscriptionTests : IntegrationTestBase
{
    public SubscriptionTests(CustomWebApplicationFactory factory) : base(factory) { }

    // 🔬 Coverage Technique: Happy Path: Verify the primary success flow — Creator configures VIP tier and User subscribes successfully (FT-16).
    [Fact]
    public async Task Subscribe_WithActiveVipTier_CreatesPendingTransaction()
    {
        // 1. Arrange: Tạo Creator hợp lệ trong DB test trước
        var creatorId = Guid.NewGuid();
        _dbContext.Users.Add(new User
        {
            Id = creatorId,
            Email = "creator_sub@orimate.com",
            PasswordHash = "hashed",
            Status = AccountStatus.Active
        });

        _dbContext.CreatorVipSettings.Add(new CreatorVipSettings
        {
            Id = Guid.NewGuid(),
            CreatorId = creatorId,
            Price = 50000,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        // Đăng nhập User khác (Learner/Subscriber)
        await AuthenticateAsAsync("User");
        _dbContext.ChangeTracker.Clear();

        var requestBody = new
        {
            CreatorId = creatorId,
            ReferenceCode = "TXN_TEST_12345"
        };

        // 2. Act
        var response = await _client.PostAsJsonAsync("/api/subscriptions", requestBody);

        // 3. Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("status").GetString().Should().Be("PendingConfirmation");
        result.GetProperty("amount").GetDecimal().Should().Be(50000);
    }

    // 🔬 Coverage Technique: Happy Path: Verify Admin payment confirmation activates VipSubscription for 30 days (BR-VIP-02 / BR-PAYMENT-01).
    [Fact]
    public async Task ConfirmPayment_ByAdmin_ActivatesVipSubscriptionSuccessfully()
    {
        // 1. Arrange: Seed đầy đủ User (Subscriber) và User (Creator) hợp lệ để thỏa mãn khóa ngoại
        var subscriberId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();

        _dbContext.Users.AddRange(
            new User { Id = subscriberId, Email = "sub@orimate.com", PasswordHash = "hash", Status = AccountStatus.Active },
            new User { Id = creatorId, Email = "creator@orimate.com", PasswordHash = "hash", Status = AccountStatus.Active }
        );

        _dbContext.Transactions.Add(new Transaction
        {
            Id = transactionId,
            UserId = subscriberId,
            CreatorId = creatorId,
            TransactionType = TransactionType.VipSubscription,
            Amount = 50000,
            PlatformFeeAmount = 5000,
            CreatorNetAmount = 45000,
            Status = TransactionStatus.PendingConfirmation,
            ReferenceCode = "REF_CONFIRM_01",
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        // Đăng nhập với quyền Admin
        await AuthenticateAsAsync("Admin");
        _dbContext.ChangeTracker.Clear();

        // 2. Act
        var response = await _client.PostAsync($"/api/subscriptions/transactions/{transactionId}/confirm", null);

        // 3. Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("status").GetString().Should().Be("Active");

        var subInDb = await _dbContext.VipSubscriptions.FirstOrDefaultAsync(s => s.TransactionId == transactionId);
        subInDb.Should().NotBeNull();
        subInDb!.EndDate.Should().BeAfter(subInDb.StartDate);
    }

    // 🔬 Coverage Technique: Error Path: Verify duplicate active subscription attempt is rejected (BR-VIP-05).
    [Fact]
    public async Task Subscribe_WhenAlreadyActive_ReturnsBadRequest()
    {
        // 1. Arrange: Seed đầy đủ User, Creator, Transaction gốc trước khi add VipSubscription
        var creatorId = Guid.NewGuid();
        var subscriberId = await AuthenticateAsAsync("User"); // Đăng nhập user hiện tại làm subscriber
        var transactionId = Guid.NewGuid();

        _dbContext.Users.Add(new User { Id = creatorId, Email = "creator_dup@orimate.com", PasswordHash = "hash", Status = AccountStatus.Active });

        _dbContext.CreatorVipSettings.Add(new CreatorVipSettings
        {
            Id = Guid.NewGuid(),
            CreatorId = creatorId,
            Price = 50000,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        _dbContext.Transactions.Add(new Transaction
        {
            Id = transactionId,
            UserId = subscriberId,
            CreatorId = creatorId,
            TransactionType = TransactionType.VipSubscription,
            Amount = 50000,
            PlatformFeeAmount = 5000,
            CreatorNetAmount = 45000,
            Status = TransactionStatus.Confirmed,
            ReferenceCode = "EXISTING_TXN",
            CreatedAt = DateTime.UtcNow
        });

        _dbContext.VipSubscriptions.Add(new VipSubscription
        {
            Id = Guid.NewGuid(),
            SubscriberId = subscriberId,
            CreatorId = creatorId,
            TransactionId = transactionId, // Phải trỏ đúng TransactionId tồn tại trong DB
            StartDate = DateTime.UtcNow.AddDays(-5),
            EndDate = DateTime.UtcNow.AddDays(25),
            Status = SubscriptionStatus.Active,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var requestBody = new
        {
            CreatorId = creatorId,
            ReferenceCode = "DUPLICATE_TXN"
        };

        // 2. Act
        var response = await _client.PostAsJsonAsync("/api/subscriptions", requestBody);

        // 3. Assert: Phải từ chối với mã 400 BadRequest (BR-VIP-05)
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // 🔬 Coverage Technique: Error Path: Verify regular user cannot confirm payment (Admin-only restriction).
    [Fact]
    public async Task ConfirmPayment_ByRegularUser_ReturnsForbidden()
    {
        // 1. Arrange
        await AuthenticateAsAsync("User"); // Quyền user thường
        var transactionId = Guid.NewGuid();

        // 2. Act
        var response = await _client.PostAsync($"/api/subscriptions/transactions/{transactionId}/confirm", null);

        // 3. Assert: Phải bị chặn với mã 403 Forbidden
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
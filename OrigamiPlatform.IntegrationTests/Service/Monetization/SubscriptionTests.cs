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

    // 🔬 Coverage Technique: Happy Path: Verify the primary success flow — Creator configures VIP tier and User subscribes, returning SePay payment instructions.
    [Fact]
    public async Task Subscribe_WithActiveVipTier_ReturnsPaymentInstructions()
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

        // API mới KHÔNG truyền ReferenceCode nữa
        var requestBody = new
        {
            CreatorId = creatorId
        };

        // 2. Act
        var response = await _client.PostAsJsonAsync("/api/subscriptions", requestBody);

        // 3. Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Kiểm tra cấu trúc trả về mới (SubscribeResultDto)
        var transactionNode = result.GetProperty("transaction");
        var instructionNode = result.GetProperty("paymentInstruction");

        transactionNode.GetProperty("status").GetString().Should().Be("PendingConfirmation");
        transactionNode.GetProperty("amount").GetDecimal().Should().Be(50000);

        instructionNode.GetProperty("paymentCode").GetString().Should().NotBeNullOrEmpty();
        instructionNode.GetProperty("qrCodeUrl").GetString().Should().Contain("sepay.vn");
    }

    // 🔬 Coverage Technique: Error Path: Verify duplicate active subscription attempt is rejected (BR-VIP-05).
    [Fact]
    public async Task Subscribe_WhenAlreadyActive_ReturnsBadRequest()
    {
        // 1. Arrange: Seed đầy đủ User, Creator, Transaction gốc trước khi add VipSubscription
        var creatorId = Guid.NewGuid();
        var subscriberId = await AuthenticateAsAsync("User"); // Đăng nhập user hiện tại làm subscriber
        var transactionId = Guid.NewGuid();

        _dbContext.Users.Add(new User { Id = creatorId, Email = "creator_dup_2@orimate.com", PasswordHash = "hash", Status = AccountStatus.Active });

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
            PaymentCode = "VIP" + Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow
        });

        _dbContext.VipSubscriptions.Add(new VipSubscription
        {
            Id = Guid.NewGuid(),
            SubscriberId = subscriberId,
            CreatorId = creatorId,
            TransactionId = transactionId,
            StartDate = DateTime.UtcNow.AddDays(-5),
            EndDate = DateTime.UtcNow.AddDays(25),
            Status = SubscriptionStatus.Active,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var requestBody = new
        {
            CreatorId = creatorId
        };

        // 2. Act
        var response = await _client.PostAsJsonAsync("/api/subscriptions", requestBody);

        // 3. Assert: Phải từ chối với mã 400 BadRequest (BR-VIP-05)
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
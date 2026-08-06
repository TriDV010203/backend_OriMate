using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.Subscriptions;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.EconomyAndVip;

public class VipSubscriptionTests : IntegrationTestBase
{
    public VipSubscriptionTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<(Guid CreatorId, Guid SubscriberId, SubscribeResultDto Subscription)> SubscribeAsync()
    {
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

        var subscriberId = await AuthenticateAsAsync("User");
        var subResponse = await _client.PostAsJsonAsync("/api/subscriptions", new { CreatorId = creatorUserId });
        subResponse.EnsureSuccessStatusCode();

        var result = await subResponse.Content.ReadFromJsonAsync<SubscribeResultDto>();
        result.Should().NotBeNull();

        return (creatorUserId, subscriberId, result!);
    }

    private HttpRequestMessage BuildWebhookRequest(
        long sePayTransactionId,
        string content,
        decimal transferAmount,
        string? apiKey = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/sepay")
        {
            Content = JsonContent.Create(new
            {
                id = sePayTransactionId,
                gateway = "TestBank",
                transactionDate = "2026-08-06 10:00:00",
                accountNumber = "0123456789",
                code = (string?)null,
                content,
                transferType = "in",
                transferAmount,
                referenceCode = $"FT{sePayTransactionId}"
            })
        };
        request.Headers.TryAddWithoutValidation("Authorization", $"Apikey {apiKey ?? CustomWebApplicationFactory.SePayTestApiKey}");
        return request;
    }

    // [Happy Path & Transaction Boundary] (FT-16) - Đăng ký VIP, SePay webhook tự động xác nhận và tạo VipSubscription
    [Fact]
    public async Task Subscribe_ThenSePayWebhookMatches_AutoConfirmsAndCreatesActiveVipSubscription()
    {
        var (_, _, subscribeResult) = await SubscribeAsync();
        var transactionId = subscribeResult.Transaction.Id;
        var paymentCode = subscribeResult.PaymentInstruction.PaymentCode;

        _dbContext.ChangeTracker.Clear();
        var transaction = await _dbContext.Transactions.FindAsync(transactionId);
        transaction!.Status.Should().Be(TransactionStatus.PendingConfirmation);

        using var webhookRequest = BuildWebhookRequest(
            sePayTransactionId: 100001,
            content: $"CHUYEN TIEN {paymentCode}",
            transferAmount: subscribeResult.Transaction.Amount);

        var webhookResponse = await _client.SendAsync(webhookRequest);
        webhookResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        _dbContext.ChangeTracker.Clear();
        var updatedTx = await _dbContext.Transactions.FindAsync(transactionId);
        updatedTx!.Status.Should().Be(TransactionStatus.Confirmed);

        var vipSub = await _dbContext.VipSubscriptions.FirstOrDefaultAsync(v => v.TransactionId == transactionId);
        vipSub.Should().NotBeNull("Phải sinh ra gói VIP sau khi webhook SePay khớp giao dịch");
        vipSub!.Status.Should().Be(SubscriptionStatus.Active);
    }

    // [Security] Webhook sai Authorization header phải bị từ chối và KHÔNG xử lý payload.
    [Fact]
    public async Task SePayWebhook_WithInvalidApiKey_ReturnsUnauthorizedAndDoesNotConfirm()
    {
        var (_, _, subscribeResult) = await SubscribeAsync();
        var transactionId = subscribeResult.Transaction.Id;
        var paymentCode = subscribeResult.PaymentInstruction.PaymentCode;

        using var webhookRequest = BuildWebhookRequest(
            sePayTransactionId: 100002,
            content: $"CHUYEN TIEN {paymentCode}",
            transferAmount: subscribeResult.Transaction.Amount,
            apiKey: "wrong-key");

        var webhookResponse = await _client.SendAsync(webhookRequest);
        webhookResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        _dbContext.ChangeTracker.Clear();
        var transaction = await _dbContext.Transactions.FindAsync(transactionId);
        transaction!.Status.Should().Be(TransactionStatus.PendingConfirmation);
    }

    // [Idempotency] Webhook gửi trùng SePayTransactionId (retry) không được tạo 2 VipSubscription.
    [Fact]
    public async Task SePayWebhook_SameSePayTransactionIdTwice_IsIdempotent()
    {
        var (_, _, subscribeResult) = await SubscribeAsync();
        var transactionId = subscribeResult.Transaction.Id;
        var paymentCode = subscribeResult.PaymentInstruction.PaymentCode;

        for (var i = 0; i < 2; i++)
        {
            using var webhookRequest = BuildWebhookRequest(
                sePayTransactionId: 100003,
                content: $"CHUYEN TIEN {paymentCode}",
                transferAmount: subscribeResult.Transaction.Amount);

            var webhookResponse = await _client.SendAsync(webhookRequest);
            webhookResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        _dbContext.ChangeTracker.Clear();
        var subscriptionCount = await _dbContext.VipSubscriptions
            .CountAsync(v => v.TransactionId == transactionId);
        subscriptionCount.Should().Be(1, "Webhook trùng SePayTransactionId phải bị bỏ qua, không tạo thêm VipSubscription");
    }

    // [Error Path] Số tiền chuyển khoản không khớp Amount của Transaction -> giữ nguyên PendingConfirmation.
    [Fact]
    public async Task SePayWebhook_AmountMismatch_LeavesTransactionPending()
    {
        var (_, _, subscribeResult) = await SubscribeAsync();
        var transactionId = subscribeResult.Transaction.Id;
        var paymentCode = subscribeResult.PaymentInstruction.PaymentCode;

        using var webhookRequest = BuildWebhookRequest(
            sePayTransactionId: 100004,
            content: $"CHUYEN TIEN {paymentCode}",
            transferAmount: subscribeResult.Transaction.Amount + 1000);

        var webhookResponse = await _client.SendAsync(webhookRequest);
        webhookResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        _dbContext.ChangeTracker.Clear();
        var transaction = await _dbContext.Transactions.FindAsync(transactionId);
        transaction!.Status.Should().Be(TransactionStatus.PendingConfirmation);
    }

    // [Error Path / Boundary] (FT-16) - Cố đăng ký Creator không mở gói VIP (thiếu CreatorVipSettings)
    [Fact]
    public async Task Subscribe_ToCreatorWithoutVipSettings_ReturnsBadRequestOrNotFound()
    {
        var creatorUserId = await AuthenticateAsAsync("User");

        await AuthenticateAsAsync("User");
        var response = await _client.PostAsJsonAsync("/api/subscriptions", new { CreatorId = creatorUserId });

        // Ghi nhận thực tế code BE đang trả về 404 khi thiếu CreatorVipSettings
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }
}

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Infrastructure.Persistence;

namespace OrigamiPlatform.IntegrationTests.Controllers;

public sealed class WebhookControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public WebhookControllerIntegrationTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task ProcessSePayWebhook_ValidPayload_ConfirmsTransactionAndCreatesVip()
    {
        await _factory.ResetDatabaseAsync();
        var transaction = await SeedPendingTransactionAsync();
        using var client = _factory.CreateClient();
        var payload = BuildPayload(transaction, 700001);

        var response = await SendWebhookAsync(client, payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<SuccessResponse>())!.Success.Should().BeTrue();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storedTransaction = await db.Transactions.SingleAsync(t => t.Id == transaction.Id);
        storedTransaction.Status.Should().Be(TransactionStatus.Confirmed);
        storedTransaction.AdminNote.Should().Be("Auto-confirmed via SePay webhook");
        (await db.VipSubscriptions.CountAsync(s => s.TransactionId == transaction.Id)).Should().Be(1);
        (await db.SePayWebhookLogs.SingleAsync(l => l.SePayTransactionId == 700001))
            .MatchResult.Should().Be(SePayWebhookMatchResult.Matched);
    }

    [Fact]
    public async Task ProcessSePayWebhook_DuplicateId_ReturnsAlreadyProcessed()
    {
        await _factory.ResetDatabaseAsync();
        var transaction = await SeedPendingTransactionAsync();
        using var client = _factory.CreateClient();
        var payload = BuildPayload(transaction, 700002);

        (await SendWebhookAsync(client, payload)).StatusCode.Should().Be(HttpStatusCode.OK);
        var duplicateResponse = await SendWebhookAsync(client, payload);

        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.SePayWebhookLogs.CountAsync(l => l.SePayTransactionId == 700002)).Should().Be(1);
        (await db.VipSubscriptions.CountAsync(s => s.TransactionId == transaction.Id)).Should().Be(1);
        (await db.Transactions.SingleAsync(t => t.Id == transaction.Id)).Status
            .Should().Be(TransactionStatus.Confirmed);
    }

    [Fact]
    public async Task ProcessSePayWebhook_MissingApiKey_ReturnsUnauthorized()
    {
        await _factory.ResetDatabaseAsync();
        var transaction = await SeedPendingTransactionAsync();
        using var client = _factory.CreateClient();
        var payload = BuildPayload(transaction, 700003);

        var response = await SendWebhookAsync(client, payload, apiKey: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProcessSePayWebhook_WrongApiKey_ReturnsUnauthorized()
    {
        await _factory.ResetDatabaseAsync();
        var transaction = await SeedPendingTransactionAsync();
        using var client = _factory.CreateClient();
        var payload = BuildPayload(transaction, 700004);

        var response = await SendWebhookAsync(client, payload, "wrong-api-key");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProcessSePayWebhook_PartialPaymentAmount_UpdatesTransactionButDoesNotCreateVip()
    {
        await _factory.ResetDatabaseAsync();
        var transaction = await SeedPendingTransactionAsync();
        using var client = _factory.CreateClient();
        var payload = BuildPayload(transaction, 700005, transferAmount: 10000m);

        var response = await SendWebhookAsync(client, payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Transactions.SingleAsync(t => t.Id == transaction.Id)).Status
            .Should().Be(TransactionStatus.PendingConfirmation);
        (await db.VipSubscriptions.CountAsync(s => s.TransactionId == transaction.Id)).Should().Be(0);
        (await db.SePayWebhookLogs.SingleAsync(l => l.SePayTransactionId == 700005))
            .MatchResult.Should().Be(SePayWebhookMatchResult.AmountMismatch);
    }

    [Fact]
    public async Task ProcessSePayWebhook_OverPaymentAmount_ConfirmsTransactionAndCreatesVip()
    {
        await _factory.ResetDatabaseAsync();
        var transaction = await SeedPendingTransactionAsync();
        using var client = _factory.CreateClient();
        var payload = BuildPayload(transaction, 700006, transferAmount: 50000m);

        var response = await SendWebhookAsync(client, payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Transactions.SingleAsync(t => t.Id == transaction.Id)).Status
            .Should().Be(TransactionStatus.PendingConfirmation);
        (await db.VipSubscriptions.CountAsync(s => s.TransactionId == transaction.Id)).Should().Be(0);
        (await db.SePayWebhookLogs.SingleAsync(l => l.SePayTransactionId == 700006))
            .MatchResult.Should().Be(SePayWebhookMatchResult.AmountMismatch);
    }

    [Fact]
    public async Task ProcessSePayWebhook_InvalidPaymentCode_ReturnsNotFound()
    {
        await _factory.ResetDatabaseAsync();
        var transaction = await SeedPendingTransactionAsync();
        using var client = _factory.CreateClient();
        var payload = BuildPayload(
            transaction,
            700007,
            paymentCode: "OMVIPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");

        var response = await SendWebhookAsync(client, payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Transactions.SingleAsync(t => t.Id == transaction.Id)).Status
            .Should().Be(TransactionStatus.PendingConfirmation);
        (await db.SePayWebhookLogs.SingleAsync(l => l.SePayTransactionId == 700007))
            .MatchResult.Should().Be(SePayWebhookMatchResult.NoMatch);
    }

    private async Task<HttpResponseMessage> SendWebhookAsync(
        HttpClient client,
        object payload,
        string? apiKey = "Orimate2026")
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/sepay")
        {
            Content = JsonContent.Create(payload)
        };
        if (apiKey is not null)
            request.Headers.Add("Authorization", $"Apikey {apiKey}");
        return await client.SendAsync(request);
    }

    private static object BuildPayload(
        Transaction transaction,
        long sePayId,
        decimal? transferAmount = null,
        string? paymentCode = null) => new
    {
        id = sePayId,
        gateway = "MBBank",
        transactionDate = "2026-09-15 10:00:00",
        accountNumber = "4238659887986",
        subAccount = "",
        code = paymentCode ?? transaction.PaymentCode,
        content = paymentCode ?? transaction.PaymentCode,
        transferType = "in",
        transferAmount = transferAmount ?? transaction.Amount,
        accumulated = 0,
        referenceCode = "REF-TEST",
        description = "Integration test payment"
    };

    private async Task<Transaction> SeedPendingTransactionAsync()
    {
        var now = DateTime.UtcNow;
        var subscriber = new User
        {
            Id = Guid.NewGuid(),
            Email = $"subscriber-{Guid.NewGuid():N}@example.com",
            PasswordHash = "unused",
            Status = AccountStatus.Active,
            CreatedAt = now
        };
        var creator = new User
        {
            Id = Guid.NewGuid(),
            Email = $"creator-{Guid.NewGuid():N}@example.com",
            PasswordHash = "unused",
            Status = AccountStatus.Active,
            CreatedAt = now
        };
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = subscriber.Id,
            User = subscriber,
            CreatorId = creator.Id,
            Creator = creator,
            TransactionType = TransactionType.VipSubscription,
            Amount = 30000m,
            PlatformFeeAmount = 3000m,
            CreatorNetAmount = 27000m,
            Status = TransactionStatus.PendingConfirmation,
            PaymentCode = "OMVIP" + Guid.NewGuid().ToString("N").ToUpperInvariant(),
            CreatedAt = now
        };

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.AddRange(subscriber, creator);
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();
        return transaction;
    }

    private sealed record SuccessResponse(bool Success);
}

using FluentAssertions;
using Moq;
using OrigamiPlatform.Application.Commands.Webhooks;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.Application.UnitTests.Webhooks;

/// <summary>
/// Unit tests for <see cref="ProcessSePayWebhookHandler"/>.
/// Pattern: Arrange – Act – Assert (Given – When – Then).
/// Naming: [MethodName]_[Scenario]_[ExpectedResult]
///
/// IMPORTANT about the "Invalid Signature" scenario:
/// The handler itself does NOT perform HMAC validation — that is an
/// infrastructure/API-layer concern (middleware or controller). Once a
/// command reaches this handler, the payload has already been verified.
/// The test below therefore covers the closest equivalent domain guard:
/// a duplicate (already-processed) transaction ID, which the handler
/// rejects early by returning AlreadyProcessed instead of executing
/// any business logic.
/// </summary>
public class ProcessSePayWebhookHandlerTests
{
    // ──────────────────────────────────────────────
    // Shared mocks & helpers
    // ──────────────────────────────────────────────
    private readonly Mock<ITransactionRepository> _txRepoMock = new();
    private readonly Mock<IVipSubscriptionRepository> _vipSubRepoMock = new();
    private readonly Mock<ISePayWebhookLogRepository> _webhookLogRepoMock = new();

    private ProcessSePayWebhookHandler CreateSut() =>
        new(_txRepoMock.Object, _vipSubRepoMock.Object, _webhookLogRepoMock.Object);

    /// <summary>
    /// A valid "money in" command whose content contains a well-formed payment code.
    /// The payment code format is: OMVIP + 32 uppercase hex chars (see VipConstants).
    /// </summary>
    private static ProcessSePayWebhookCommand BuildValidInboundCommand(
        long sePayId = 123456,
        string paymentCode = "OMVIP" + "A1B2C3D4E5F60718293A4B5C6D7E8F90",
        decimal amount = 30_000m) =>
        new(
            SePayTransactionId: sePayId,
            Gateway: "VietcomBank",
            TransferType: "in",
            TransferAmount: amount,
            Content: $"Chuyen khoan {paymentCode} vip",
            Code: paymentCode,
            RawPayload: "{\"id\":123456}"
        );

    /// <summary>
    /// Builds a <see cref="Transaction"/> in PendingConfirmation state.
    /// </summary>
    private static Transaction BuildPendingTransaction(string paymentCode, decimal amount = 30_000m) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        CreatorId = Guid.NewGuid(),
        Amount = amount,
        PaymentCode = paymentCode,
        Status = TransactionStatus.PendingConfirmation,
        TransactionType = TransactionType.VipSubscription,
        CreatedAt = DateTime.UtcNow,
        PlatformFeeAmount = 3_000m,
        CreatorNetAmount = 27_000m,
    };

    // ──────────────────────────────────────────────────────────────────
    // TC-01: HandleAsync_ValidMatchedPayment_ReturnsMatchedAndCreatesSubscription
    // ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_ValidMatchedPayment_ReturnsMatchedAndCreatesSubscription()
    {
        // ── Arrange ──────────────────────────────────────────────────
        const string paymentCode = "OMVIP" + "A1B2C3D4E5F60718293A4B5C6D7E8F90";
        const decimal amount = 30_000m;
        var command = BuildValidInboundCommand(paymentCode: paymentCode, amount: amount);
        var pendingTx = BuildPendingTransaction(paymentCode, amount);

        _webhookLogRepoMock
            .Setup(r => r.ExistsBySePayTransactionIdAsync(command.SePayTransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // not seen before

        _txRepoMock
            .Setup(r => r.GetByPaymentCodeAsync(paymentCode.ToUpperInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingTx);

        _txRepoMock
            .Setup(r => r.UpdateAsync(pendingTx, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _vipSubRepoMock
            .Setup(r => r.AddAsync(It.IsAny<VipSubscription>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _webhookLogRepoMock
            .Setup(r => r.AddAsync(It.IsAny<SePayWebhookLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // ── Act ───────────────────────────────────────────────────────
        var result = await sut.HandleAsync(command);

        // ── Assert ────────────────────────────────────────────────────
        result.Should().Be(SePayWebhookMatchResult.Matched);

        // Transaction must be confirmed
        pendingTx.Status.Should().Be(TransactionStatus.Confirmed);
        pendingTx.ConfirmedAt.Should().NotBeNull();
        pendingTx.AdminNote.Should().Contain("Auto-confirmed");

        // VIP subscription must be created
        _vipSubRepoMock.Verify(r => r.AddAsync(
            It.Is<VipSubscription>(s =>
                s.SubscriberId == pendingTx.UserId &&
                s.CreatorId == pendingTx.CreatorId!.Value &&
                s.Status == SubscriptionStatus.Active),
            It.IsAny<CancellationToken>()), Times.Once);

        // Webhook log must always be recorded
        _webhookLogRepoMock.Verify(r => r.AddAsync(
            It.Is<SePayWebhookLog>(l =>
                l.SePayTransactionId == command.SePayTransactionId &&
                l.MatchResult == SePayWebhookMatchResult.Matched &&
                l.ProcessedAt.HasValue),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ──────────────────────────────────────────────────────────────────
    // TC-02: HandleAsync_AmountMismatch_ReturnsAmountMismatchAndSkipsSubscription
    // ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_AmountMismatch_ReturnsAmountMismatchAndSkipsSubscription()
    {
        // ── Arrange ──────────────────────────────────────────────────
        const string paymentCode = "OMVIP" + "A1B2C3D4E5F60718293A4B5C6D7E8F90";
        var pendingTx = BuildPendingTransaction(paymentCode, amount: 30_000m);

        // Webhook reports a different amount
        var command = BuildValidInboundCommand(paymentCode: paymentCode, amount: 10_000m);

        _webhookLogRepoMock
            .Setup(r => r.ExistsBySePayTransactionIdAsync(command.SePayTransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _txRepoMock
            .Setup(r => r.GetByPaymentCodeAsync(paymentCode.ToUpperInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingTx);

        _webhookLogRepoMock
            .Setup(r => r.AddAsync(It.IsAny<SePayWebhookLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // ── Act ───────────────────────────────────────────────────────
        var result = await sut.HandleAsync(command);

        // ── Assert ────────────────────────────────────────────────────
        result.Should().Be(SePayWebhookMatchResult.AmountMismatch);

        // No subscription created, no transaction update
        _vipSubRepoMock.Verify(r => r.AddAsync(It.IsAny<VipSubscription>(), It.IsAny<CancellationToken>()), Times.Never);
        _txRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);

        // Log must still be saved with the mismatch result
        _webhookLogRepoMock.Verify(r => r.AddAsync(
            It.Is<SePayWebhookLog>(l => l.MatchResult == SePayWebhookMatchResult.AmountMismatch && !l.ProcessedAt.HasValue),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ──────────────────────────────────────────────────────────────────
    // TC-03: HandleAsync_NoPaymentCodeInPayload_ReturnsNoMatch
    // ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_NoPaymentCodeInPayload_ReturnsNoMatch()
    {
        // ── Arrange ──────────────────────────────────────────────────
        var command = new ProcessSePayWebhookCommand(
            SePayTransactionId: 999,
            Gateway: "Techcombank",
            TransferType: "in",
            TransferAmount: 50_000m,
            Content: "Random bank transfer without any code",
            Code: null,                // <── no structured code
            RawPayload: "{\"id\":999}"
        );

        _webhookLogRepoMock
            .Setup(r => r.ExistsBySePayTransactionIdAsync(command.SePayTransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _webhookLogRepoMock
            .Setup(r => r.AddAsync(It.IsAny<SePayWebhookLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // ── Act ───────────────────────────────────────────────────────
        var result = await sut.HandleAsync(command);

        // ── Assert ────────────────────────────────────────────────────
        result.Should().Be(SePayWebhookMatchResult.NoMatch);

        _txRepoMock.Verify(r => r.GetByPaymentCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _vipSubRepoMock.Verify(r => r.AddAsync(It.IsAny<VipSubscription>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ──────────────────────────────────────────────────────────────────
    // TC-04: HandleAsync_DuplicateSePayTransactionId_ReturnsAlreadyProcessed
    //
    // This is the "Invalid / Replayed Webhook" guard inside the handler.
    // (HMAC/signature validation happens at the API layer before the
    //  handler is ever invoked — outside the Application layer scope.)
    // ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_DuplicateSePayTransactionId_ReturnsAlreadyProcessed()
    {
        // ── Arrange ──────────────────────────────────────────────────
        var command = BuildValidInboundCommand(sePayId: 777);

        _webhookLogRepoMock
            .Setup(r => r.ExistsBySePayTransactionIdAsync(777, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // <── same transaction ID seen before

        var sut = CreateSut();

        // ── Act ───────────────────────────────────────────────────────
        var result = await sut.HandleAsync(command);

        // ── Assert ────────────────────────────────────────────────────
        result.Should().Be(SePayWebhookMatchResult.AlreadyProcessed);

        // No side-effects should happen for a replayed webhook
        _txRepoMock.Verify(r => r.GetByPaymentCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _txRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
        _vipSubRepoMock.Verify(r => r.AddAsync(It.IsAny<VipSubscription>(), It.IsAny<CancellationToken>()), Times.Never);
        _webhookLogRepoMock.Verify(r => r.AddAsync(It.IsAny<SePayWebhookLog>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ──────────────────────────────────────────────────────────────────
    // TC-05: HandleAsync_TransferTypeIsOut_ReturnsNoMatch
    // Outbound transfers must always be ignored.
    // ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_TransferTypeIsOut_ReturnsNoMatch()
    {
        // ── Arrange ──────────────────────────────────────────────────
        var command = new ProcessSePayWebhookCommand(
            SePayTransactionId: 101,
            Gateway: "BIDV",
            TransferType: "out",          // <── outbound transfer
            TransferAmount: 30_000m,
            Content: "OMVIPA1B2C3D4E5F60718293A4B5C6D7E8F90",
            Code: "OMVIP" + "A1B2C3D4E5F60718293A4B5C6D7E8F90",
            RawPayload: "{\"id\":101}"
        );

        _webhookLogRepoMock
            .Setup(r => r.ExistsBySePayTransactionIdAsync(101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _webhookLogRepoMock
            .Setup(r => r.AddAsync(It.IsAny<SePayWebhookLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // ── Act ───────────────────────────────────────────────────────
        var result = await sut.HandleAsync(command);

        // ── Assert ────────────────────────────────────────────────────
        result.Should().Be(SePayWebhookMatchResult.NoMatch);

        _txRepoMock.Verify(r => r.GetByPaymentCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _vipSubRepoMock.Verify(r => r.AddAsync(It.IsAny<VipSubscription>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

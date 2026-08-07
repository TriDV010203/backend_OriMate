using System.Text.RegularExpressions;
using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Commands.Webhooks;

/// <summary>
/// Handles SePay's "money in" webhook: matches the transfer to a pending VIP Transaction by
/// PaymentCode, auto-confirms it and activates the VipSubscription. BR-PAYMENT-01 (Giai đoạn 2) —
/// replaces the old manual Admin Confirm/Reject flow entirely.
/// </summary>
public class ProcessSePayWebhookHandler
{
    private const int SubscriptionDurationDays = 30;

    private static readonly Regex PaymentCodeInContentPattern =
        new($"{VipConstants.PaymentCodePrefix}[0-9A-F]{{32}}", RegexOptions.Compiled);

    private readonly ITransactionRepository _transactions;
    private readonly IVipSubscriptionRepository _vipSubscriptions;
    private readonly ISePayWebhookLogRepository _webhookLogs;

    public ProcessSePayWebhookHandler(
        ITransactionRepository transactions,
        IVipSubscriptionRepository vipSubscriptions,
        ISePayWebhookLogRepository webhookLogs)
        => (_transactions, _vipSubscriptions, _webhookLogs) = (transactions, vipSubscriptions, webhookLogs);

    public async Task<SePayWebhookMatchResult> HandleAsync(
        ProcessSePayWebhookCommand command,
        CancellationToken ct = default)
    {
        if (await _webhookLogs.ExistsBySePayTransactionIdAsync(command.SePayTransactionId, ct))
            return SePayWebhookMatchResult.AlreadyProcessed;

        var result = SePayWebhookMatchResult.NoMatch;
        Transaction? transaction = null;

        if (string.Equals(command.TransferType, "in", StringComparison.OrdinalIgnoreCase))
        {
            var paymentCode = ExtractPaymentCode(command.Code, command.Content);
            if (paymentCode is not null)
            {
                var candidate = await _transactions.GetByPaymentCodeAsync(paymentCode, ct);
                if (candidate is not null
                    && candidate.Status == TransactionStatus.PendingConfirmation
                    && candidate.CreatorId is not null)
                {
                    transaction = candidate;
                    result = candidate.Amount == command.TransferAmount
                        ? SePayWebhookMatchResult.Matched
                        : SePayWebhookMatchResult.AmountMismatch;
                }
            }
        }

        var now = DateTime.UtcNow;

        if (result == SePayWebhookMatchResult.Matched && transaction is not null)
        {
            transaction.Status = TransactionStatus.Confirmed;
            transaction.ConfirmedBy = null;
            transaction.ConfirmedAt = now;
            transaction.AdminNote = "Auto-confirmed via SePay webhook";
            transaction.UpdatedAt = now;
            await _transactions.UpdateAsync(transaction, ct);

            var subscription = new VipSubscription
            {
                Id = Guid.NewGuid(),
                SubscriberId = transaction.UserId,
                CreatorId = transaction.CreatorId!.Value,
                TransactionId = transaction.Id,
                StartDate = now,
                EndDate = now.AddDays(SubscriptionDurationDays),
                Status = SubscriptionStatus.Active,
                CreatedAt = now
            };
            await _vipSubscriptions.AddAsync(subscription, ct);
        }

        await _webhookLogs.AddAsync(new SePayWebhookLog
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction?.Id,
            SePayTransactionId = command.SePayTransactionId,
            Gateway = command.Gateway,
            TransferAmount = command.TransferAmount,
            Content = command.Content,
            MatchResult = result,
            RawPayload = command.RawPayload,
            ProcessedAt = result == SePayWebhookMatchResult.Matched ? now : null,
            CreatedAt = now
        }, ct);

        return result;
    }

    private static string? ExtractPaymentCode(string? code, string content)
    {
        if (!string.IsNullOrWhiteSpace(code)
            && code.StartsWith(VipConstants.PaymentCodePrefix, StringComparison.OrdinalIgnoreCase))
            return code.ToUpperInvariant();

        var match = PaymentCodeInContentPattern.Match(content.ToUpperInvariant());
        return match.Success ? match.Value : null;
    }
}

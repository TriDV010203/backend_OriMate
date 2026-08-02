using OrigamiPlatform.Application.DTOs.Subscriptions;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Subscriptions;

public class ConfirmPaymentHandler
{
    private const int SubscriptionDurationDays = 30;

    private readonly ITransactionRepository _transactions;
    private readonly IVipSubscriptionRepository _vipSubscriptions;

    public ConfirmPaymentHandler(
        ITransactionRepository transactions,
        IVipSubscriptionRepository vipSubscriptions)
        => (_transactions, _vipSubscriptions) = (transactions, vipSubscriptions);

    public async Task<VipSubscriptionDto> HandleAsync(ConfirmPaymentCommand command, CancellationToken ct = default)
    {
        var transaction = await _transactions.GetByIdAsync(command.TransactionId, ct)
            ?? throw new NotFoundException("Transaction not found.");

        if (transaction.Status != TransactionStatus.PendingConfirmation)
            throw new DomainException("This transaction has already been processed.");

        if (transaction.CreatorId is null)
            throw new DomainException("Transaction is missing creator information.");

        var now = DateTime.UtcNow;

        transaction.Status = TransactionStatus.Confirmed;
        transaction.ConfirmedBy = command.AdminId;
        transaction.ConfirmedAt = now;
        transaction.UpdatedAt = now;

        await _transactions.UpdateAsync(transaction, ct);

        // BR-VIP-02: 30-day fixed subscription, no auto-renew.
        var subscription = new VipSubscription
        {
            Id = Guid.NewGuid(),
            SubscriberId = transaction.UserId,
            CreatorId = transaction.CreatorId.Value,
            TransactionId = transaction.Id,
            StartDate = now,
            EndDate = now.AddDays(SubscriptionDurationDays),
            Status = SubscriptionStatus.Active,
            CreatedAt = now
        };

        await _vipSubscriptions.AddAsync(subscription, ct);

        return subscription.ToDto();
    }
}

using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.DTOs.Subscriptions;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Subscriptions;

public class SubscribeHandler
{
    private const int MaxReferenceCodeLength = 100;

    private readonly ICreatorVipSettingsRepository _settings;
    private readonly ITransactionRepository _transactions;
    private readonly IVipSubscriptionRepository _vipSubscriptions;

    public SubscribeHandler(
        ICreatorVipSettingsRepository settings,
        ITransactionRepository transactions,
        IVipSubscriptionRepository vipSubscriptions)
        => (_settings, _transactions, _vipSubscriptions) = (settings, transactions, vipSubscriptions);

    public async Task<TransactionDto> HandleAsync(SubscribeCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.ReferenceCode))
            throw new DomainException("Reference code is required.");

        if (command.ReferenceCode.Length > MaxReferenceCodeLength)
            throw new DomainException($"Reference code must not exceed {MaxReferenceCodeLength} characters.");

        // BR-VIP-03: creator must have an active VIP tier before anyone can subscribe.
        var settings = await _settings.GetByCreatorIdAsync(command.CreatorId, ct)
            ?? throw new NotFoundException("This creator does not offer VIP subscriptions.");

        if (!settings.IsActive)
            throw new DomainException("This creator's VIP subscription is not currently active.");

        var hasActiveSubscription = await _vipSubscriptions.HasActiveSubscriptionAsync(
            command.SubscriberId, command.CreatorId, ct);
        if (hasActiveSubscription)
            throw new DomainException("You already have an active VIP subscription with this Creator.");

        // BR-VIP-05: price is platform-fixed, not the creator-configured settings.Price.
        var amount = VipConstants.FixedPriceVnd;
        var platformFee = Math.Round(amount * VipConstants.PlatformCommissionRate, 2);
        var creatorNet = amount - platformFee;

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = command.SubscriberId,
            CreatorId = command.CreatorId,
            TransactionType = TransactionType.VipSubscription,
            Amount = amount,
            PlatformFeeAmount = platformFee,
            CreatorNetAmount = creatorNet,
            Status = TransactionStatus.PendingConfirmation,
            ReferenceCode = command.ReferenceCode,
            CreatedAt = DateTime.UtcNow
        };

        await _transactions.AddAsync(transaction, ct);

        return transaction.ToDto();
    }
}

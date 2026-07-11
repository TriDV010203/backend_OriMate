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

    public SubscribeHandler(ICreatorVipSettingsRepository settings, ITransactionRepository transactions)
        => (_settings, _transactions) = (settings, transactions);

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

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = command.SubscriberId,
            CreatorId = command.CreatorId,
            TransactionType = TransactionType.VipSubscription,
            Amount = settings.Price,
            Status = TransactionStatus.PendingConfirmation,
            ReferenceCode = command.ReferenceCode,
            CreatedAt = DateTime.UtcNow
        };

        await _transactions.AddAsync(transaction, ct);

        return transaction.ToDto();
    }
}

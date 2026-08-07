using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.DTOs.Subscriptions;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Subscriptions;

public class SubscribeHandler
{
    private readonly ICreatorVipSettingsRepository _settings;
    private readonly ITransactionRepository _transactions;
    private readonly IVipSubscriptionRepository _vipSubscriptions;
    private readonly IBankAccountInfoProvider _bankAccount;

    public SubscribeHandler(
        ICreatorVipSettingsRepository settings,
        ITransactionRepository transactions,
        IVipSubscriptionRepository vipSubscriptions,
        IBankAccountInfoProvider bankAccount)
        => (_settings, _transactions, _vipSubscriptions, _bankAccount) = (settings, transactions, vipSubscriptions, bankAccount);

    public async Task<SubscribeResultDto> HandleAsync(SubscribeCommand command, CancellationToken ct = default)
    {
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
            CreatedAt = DateTime.UtcNow
        };
        // System-generated, matched against SePay webhook transfer content — derived from the
        // Transaction's own PK so it is guaranteed unique without a collision-retry loop.
        transaction.PaymentCode = $"{VipConstants.PaymentCodePrefix}{transaction.Id:N}".ToUpperInvariant();

        await _transactions.AddAsync(transaction, ct);

        var paymentInstruction = new PaymentInstructionDto(
            _bankAccount.AccountNumber,
            _bankAccount.BankName,
            _bankAccount.BankBin,
            _bankAccount.AccountHolderName,
            transaction.PaymentCode,
            transaction.Amount,
            BuildQrCodeUrl(transaction));

        return new SubscribeResultDto(transaction.ToDto(), paymentInstruction);
    }

    private string BuildQrCodeUrl(Transaction transaction)
        => $"https://qr.sepay.vn/img?acc={Uri.EscapeDataString(_bankAccount.AccountNumber)}" +
           $"&bank={Uri.EscapeDataString(_bankAccount.BankBin)}" +
           $"&amount={(long)transaction.Amount}" +
           $"&des={Uri.EscapeDataString(transaction.PaymentCode)}";
}

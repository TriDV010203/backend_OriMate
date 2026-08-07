namespace OrigamiPlatform.Application.DTOs.Subscriptions;

public record TransactionDto(
    Guid Id,
    Guid UserId,
    Guid? CreatorId,
    string TransactionType,
    decimal Amount,
    decimal PlatformFeeAmount,
    decimal CreatorNetAmount,
    string Status,
    string PaymentCode,
    Guid? ConfirmedBy,
    DateTime? ConfirmedAt,
    string? AdminNote,
    DateTime CreatedAt
);

/// <summary>Enriched transaction shape for the admin ledger/pending-confirmation queue.</summary>
public record AdminTransactionDto(
    Guid Id,
    Guid UserId,
    string SubscriberDisplayName,
    string? SubscriberAvatarUrl,
    Guid? CreatorId,
    string? CreatorDisplayName,
    string? CreatorAvatarUrl,
    string TransactionType,
    decimal Amount,
    decimal PlatformFeeAmount,
    decimal CreatorNetAmount,
    string Status,
    string PaymentCode,
    Guid? ConfirmedBy,
    DateTime? ConfirmedAt,
    string? AdminNote,
    DateTime CreatedAt
);

/// <summary>Bank transfer instructions shown to the buyer right after a Transaction is created — SePay auto-matches by PaymentCode in the transfer content.</summary>
public record PaymentInstructionDto(
    string BankAccountNumber,
    string BankName,
    string BankBin,
    string AccountHolderName,
    string PaymentCode,
    decimal Amount,
    string QrCodeUrl
);

/// <summary>Result of Subscribe — the created Transaction plus how to pay for it.</summary>
public record SubscribeResultDto(
    TransactionDto Transaction,
    PaymentInstructionDto PaymentInstruction
);

public record PlatformRevenueDto(
    decimal TotalGrossRevenue,
    decimal TotalCommissionCollected,
    decimal TotalNetPaidToCreators,
    int ConfirmedCount,
    int PendingCount,
    int RejectedCount,
    int ActiveSubscriptionCount
);

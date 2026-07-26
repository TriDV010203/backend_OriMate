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
    string? ReferenceCode,
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
    string? ReferenceCode,
    Guid? ConfirmedBy,
    DateTime? ConfirmedAt,
    string? AdminNote,
    DateTime CreatedAt
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

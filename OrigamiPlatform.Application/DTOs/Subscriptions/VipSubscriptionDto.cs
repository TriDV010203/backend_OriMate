namespace OrigamiPlatform.Application.DTOs.Subscriptions;

public record VipSubscriptionDto(
    Guid Id,
    Guid SubscriberId,
    Guid CreatorId,
    Guid TransactionId,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    DateTime CreatedAt
);

/// <summary>Enriched subscription shape for the buyer's own "VIP của tôi" list — includes
/// creator display info and remaining days so the frontend needs no extra round-trips.</summary>
public record MySubscriptionDto(
    Guid Id,
    Guid SubscriberId,
    Guid CreatorId,
    string CreatorDisplayName,
    string? CreatorAvatarUrl,
    Guid TransactionId,
    decimal Price,
    DateTime StartDate,
    DateTime EndDate,
    int DaysRemaining,
    string Status,
    DateTime CreatedAt
);

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

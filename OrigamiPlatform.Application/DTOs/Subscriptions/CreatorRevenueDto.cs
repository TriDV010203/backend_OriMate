namespace OrigamiPlatform.Application.DTOs.Subscriptions;

public record CreatorSubscriberDto(
    Guid SubscriberId,
    string DisplayName,
    string? AvatarUrl,
    DateTime StartDate,
    DateTime EndDate,
    int DaysRemaining
);

public record CreatorRevenueDto(
    Guid CreatorId,
    int ActiveSubscriberCount,
    int PendingCount,
    decimal NetRevenueThisMonth,
    decimal NetRevenueAllTime,
    DateTime PeriodStart,
    DateTime PeriodEndExclusive,
    IReadOnlyList<CreatorSubscriberDto> Subscribers
);

namespace OrigamiPlatform.Application.DTOs.Subscriptions;

public record CreatorRevenueDto(
    Guid CreatorId,
    int ActiveSubscriberCount,
    decimal ConfirmedRevenueThisMonth,
    DateTime PeriodStart,
    DateTime PeriodEndExclusive
);

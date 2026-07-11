using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.DTOs.Subscriptions;

public static class SubscriptionMapping
{
    public static CreatorVipSettingsDto ToDto(this CreatorVipSettings settings)
        => new(
            settings.Id,
            settings.CreatorId,
            settings.Price,
            settings.IsActive,
            settings.CreatedAt,
            settings.UpdatedAt);

    public static TransactionDto ToDto(this Transaction transaction)
        => new(
            transaction.Id,
            transaction.UserId,
            transaction.CreatorId,
            transaction.TransactionType.ToString(),
            transaction.Amount,
            transaction.Status.ToString(),
            transaction.ReferenceCode,
            transaction.ConfirmedBy,
            transaction.ConfirmedAt,
            transaction.AdminNote,
            transaction.CreatedAt);

    public static VipSubscriptionDto ToDto(this VipSubscription subscription)
        => new(
            subscription.Id,
            subscription.SubscriberId,
            subscription.CreatorId,
            subscription.TransactionId,
            subscription.StartDate,
            subscription.EndDate,
            subscription.EffectiveStatus().ToString(),
            subscription.CreatedAt);

    public static Domain.Enums.SubscriptionStatus EffectiveStatus(this VipSubscription subscription)
        => subscription.Status == Domain.Enums.SubscriptionStatus.Active && subscription.EndDate > DateTime.UtcNow
            ? Domain.Enums.SubscriptionStatus.Active
            : Domain.Enums.SubscriptionStatus.Expired;
}

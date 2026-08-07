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
            transaction.PlatformFeeAmount,
            transaction.CreatorNetAmount,
            transaction.Status.ToString(),
            transaction.PaymentCode,
            transaction.ConfirmedBy,
            transaction.ConfirmedAt,
            transaction.AdminNote,
            transaction.CreatedAt);

    /// <summary>Requires Transaction.User and Transaction.Creator (with Profile) to be loaded.</summary>
    public static AdminTransactionDto ToAdminDto(this Transaction transaction)
        => new(
            transaction.Id,
            transaction.UserId,
            transaction.User.Profile?.DisplayName ?? transaction.User.Email,
            transaction.User.Profile?.AvatarUrl,
            transaction.CreatorId,
            transaction.Creator?.Profile?.DisplayName ?? transaction.Creator?.Email,
            transaction.Creator?.Profile?.AvatarUrl,
            transaction.TransactionType.ToString(),
            transaction.Amount,
            transaction.PlatformFeeAmount,
            transaction.CreatorNetAmount,
            transaction.Status.ToString(),
            transaction.PaymentCode,
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

    /// <summary>Requires Subscription.Creator (with Profile) and Subscription.Transaction to be loaded.</summary>
    public static MySubscriptionDto ToMyDto(this VipSubscription subscription)
        => new(
            subscription.Id,
            subscription.SubscriberId,
            subscription.CreatorId,
            subscription.Creator.Profile?.DisplayName ?? subscription.Creator.Email,
            subscription.Creator.Profile?.AvatarUrl,
            subscription.TransactionId,
            subscription.Transaction.Amount,
            subscription.StartDate,
            subscription.EndDate,
            DaysRemaining(subscription.EndDate),
            subscription.EffectiveStatus().ToString(),
            subscription.CreatedAt);

    /// <summary>Requires Subscription.Subscriber (with Profile) to be loaded.</summary>
    public static CreatorSubscriberDto ToSubscriberDto(this VipSubscription subscription)
        => new(
            subscription.SubscriberId,
            subscription.Subscriber.Profile?.DisplayName ?? subscription.Subscriber.Email,
            subscription.Subscriber.Profile?.AvatarUrl,
            subscription.StartDate,
            subscription.EndDate,
            DaysRemaining(subscription.EndDate));

    public static Domain.Enums.SubscriptionStatus EffectiveStatus(this VipSubscription subscription)
        => subscription.Status == Domain.Enums.SubscriptionStatus.Active && subscription.EndDate > DateTime.UtcNow
            ? Domain.Enums.SubscriptionStatus.Active
            : Domain.Enums.SubscriptionStatus.Expired;

    private static int DaysRemaining(DateTime endDate)
        => Math.Max(0, (int)Math.Ceiling((endDate - DateTime.UtcNow).TotalDays));
}

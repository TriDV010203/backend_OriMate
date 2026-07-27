using OrigamiPlatform.Application.DTOs.Subscriptions;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Queries.Subscriptions;

public class GetCreatorRevenueHandler
{
    private readonly IVipSubscriptionRepository _vipSubscriptions;
    private readonly ITransactionRepository _transactions;

    public GetCreatorRevenueHandler(
        IVipSubscriptionRepository vipSubscriptions,
        ITransactionRepository transactions)
        => (_vipSubscriptions, _transactions) = (vipSubscriptions, transactions);

    public async Task<CreatorRevenueDto> HandleAsync(
        GetCreatorRevenueQuery query,
        CancellationToken ct = default)
    {
        // FT-17: a creator may only view their own revenue dashboard.
        if (query.RequestingUserId != query.CreatorId)
            throw new ForbiddenException("You can only view your own revenue dashboard.");

        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1);

        var activeSubscriberCount = await _vipSubscriptions.CountActiveSubscribersAsync(query.CreatorId, ct);
        var pendingCount = await _transactions.CountPendingByCreatorAsync(query.CreatorId, ct);
        var netRevenueThisMonth = await _transactions.GetConfirmedRevenueAsync(
            query.CreatorId,
            periodStart,
            periodEnd,
            ct);
        var netRevenueAllTime = await _transactions.GetTotalConfirmedRevenueAsync(query.CreatorId, ct);

        var subscribers = await _vipSubscriptions.GetActiveSubscribersByCreatorAsync(query.CreatorId, ct);
        var subscriberDtos = subscribers.Select(s => s.ToSubscriberDto()).ToList();

        return new CreatorRevenueDto(
            query.CreatorId,
            activeSubscriberCount,
            pendingCount,
            netRevenueThisMonth,
            netRevenueAllTime,
            periodStart,
            periodEnd,
            subscriberDtos);
    }
}

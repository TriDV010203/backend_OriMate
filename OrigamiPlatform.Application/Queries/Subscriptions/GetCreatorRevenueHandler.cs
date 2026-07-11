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
        var confirmedRevenue = await _transactions.GetConfirmedRevenueAsync(
            query.CreatorId,
            periodStart,
            periodEnd,
            ct);

        return new CreatorRevenueDto(
            query.CreatorId,
            activeSubscriberCount,
            confirmedRevenue,
            periodStart,
            periodEnd);
    }
}

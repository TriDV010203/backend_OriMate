using OrigamiPlatform.Application.DTOs.Subscriptions;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.Subscriptions;

public class GetPlatformRevenueHandler
{
    private readonly ITransactionRepository _transactions;
    private readonly IVipSubscriptionRepository _vipSubscriptions;

    public GetPlatformRevenueHandler(
        ITransactionRepository transactions,
        IVipSubscriptionRepository vipSubscriptions)
        => (_transactions, _vipSubscriptions) = (transactions, vipSubscriptions);

    public async Task<PlatformRevenueDto> HandleAsync(
        GetPlatformRevenueQuery query,
        CancellationToken ct = default)
    {
        var summary = await _transactions.GetPlatformSummaryAsync(ct);
        var activeSubscriptionCount = await _vipSubscriptions.CountAllActiveSubscriptionsAsync(ct);

        return new PlatformRevenueDto(
            summary.TotalGrossRevenue,
            summary.TotalCommission,
            summary.TotalNetPaidToCreators,
            summary.ConfirmedCount,
            summary.PendingCount,
            summary.RejectedCount,
            activeSubscriptionCount);
    }
}

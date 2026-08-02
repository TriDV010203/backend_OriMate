using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.DTOs.Subscriptions;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.Subscriptions;

public class GetMySubscriptionsHandler
{
    private readonly IVipSubscriptionRepository _vipSubscriptions;

    public GetMySubscriptionsHandler(IVipSubscriptionRepository vipSubscriptions)
        => _vipSubscriptions = vipSubscriptions;

    public async Task<PagedResult<MySubscriptionDto>> HandleAsync(
        GetMySubscriptionsQuery query,
        CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var (items, totalCount) = await _vipSubscriptions.GetBySubscriberAsync(
            query.SubscriberId,
            page,
            pageSize,
            ct);

        var dtos = items.Select(s => s.ToMyDto()).ToList();

        return new PagedResult<MySubscriptionDto>(
            dtos,
            totalCount,
            page,
            pageSize,
            (int)Math.Ceiling(totalCount / (double)pageSize));
    }
}

using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IVipSubscriptionRepository
{
    Task<bool> HasActiveSubscriptionAsync(Guid subscriberId, Guid creatorId, CancellationToken ct = default);
    Task<HashSet<Guid>> GetSubscribedCreatorIdsAsync(Guid subscriberId, CancellationToken ct = default);

    Task AddAsync(VipSubscription subscription, CancellationToken ct = default);

    Task<(IEnumerable<VipSubscription> Items, int TotalCount)> GetBySubscriberAsync(
        Guid subscriberId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<int> CountActiveSubscribersAsync(Guid creatorId, CancellationToken ct = default);
}

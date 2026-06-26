namespace OrigamiPlatform.Application.Interfaces;

public interface IVipSubscriptionRepository
{
    Task<bool> HasActiveSubscriptionAsync(Guid subscriberId, Guid creatorId, CancellationToken ct = default);
    Task<HashSet<Guid>> GetSubscribedCreatorIdsAsync(Guid subscriberId, CancellationToken ct = default);
}

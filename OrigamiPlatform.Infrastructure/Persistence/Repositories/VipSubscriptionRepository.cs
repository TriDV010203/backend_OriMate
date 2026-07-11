using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class VipSubscriptionRepository : IVipSubscriptionRepository
{
    private readonly AppDbContext _db;

    public VipSubscriptionRepository(AppDbContext db) => _db = db;

    public Task<bool> HasActiveSubscriptionAsync(Guid subscriberId, Guid creatorId, CancellationToken ct = default)
        => _db.VipSubscriptions
            .AnyAsync(s => s.SubscriberId == subscriberId
                        && s.CreatorId == creatorId
                        && s.Status == SubscriptionStatus.Active
                        && s.EndDate > DateTime.UtcNow, ct);

    public async Task<HashSet<Guid>> GetSubscribedCreatorIdsAsync(Guid subscriberId, CancellationToken ct = default)
    {
        var ids = await _db.VipSubscriptions
            .Where(s => s.SubscriberId == subscriberId
                     && s.Status == SubscriptionStatus.Active
                     && s.EndDate > DateTime.UtcNow)
            .Select(s => s.CreatorId)
            .ToListAsync(ct);
        return ids.ToHashSet();
    }

    public async Task AddAsync(VipSubscription subscription, CancellationToken ct = default)
    {
        _db.VipSubscriptions.Add(subscription);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(IEnumerable<VipSubscription> Items, int TotalCount)> GetBySubscriberAsync(
        Guid subscriberId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.VipSubscriptions
            .Where(s => s.SubscriberId == subscriberId)
            .OrderByDescending(s => s.CreatedAt);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public Task<int> CountActiveSubscribersAsync(Guid creatorId, CancellationToken ct = default)
        => _db.VipSubscriptions
            .CountAsync(s => s.CreatorId == creatorId
                          && s.Status == SubscriptionStatus.Active
                          && s.EndDate > DateTime.UtcNow, ct);
}

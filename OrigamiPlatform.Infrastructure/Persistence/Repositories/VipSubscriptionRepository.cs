using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
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
}

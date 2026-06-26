using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class FamilySubscriptionRepository : IFamilySubscriptionRepository
{
    private readonly AppDbContext _db;

    public FamilySubscriptionRepository(AppDbContext db) => _db = db;

    public Task<FamilySubscription?> GetActiveByUserAsync(Guid userId, CancellationToken ct = default)
        => _db.FamilySubscriptions
            .FirstOrDefaultAsync(
                s => s.UserId == userId
                    && s.Status == SubscriptionStatus.Active
                    && s.EndDate > DateTime.UtcNow,
                ct);
}

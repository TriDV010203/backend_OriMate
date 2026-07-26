using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class UserBadgeRepository : IUserBadgeRepository
{
    private readonly AppDbContext _db;

    public UserBadgeRepository(AppDbContext db) => _db = db;

    public Task<bool> ExistsAsync(Guid userId, Guid badgeId, CancellationToken ct = default)
        => _db.UserBadges.AnyAsync(ub => ub.UserId == userId && ub.BadgeId == badgeId, ct);

    public async Task AddAsync(UserBadge userBadge, CancellationToken ct = default)
    {
        _db.UserBadges.Add(userBadge);
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<UserBadge>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => _db.UserBadges
            .Include(ub => ub.Badge)
            .Where(ub => ub.UserId == userId)
            .OrderByDescending(ub => ub.EarnedAt)
            .ToListAsync(ct);
}

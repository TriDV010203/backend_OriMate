using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class BadgeRepository : IBadgeRepository
{
    private readonly AppDbContext _db;

    public BadgeRepository(AppDbContext db) => _db = db;

    public Task<List<Badge>> GetAllActiveAsync(CancellationToken ct = default)
        => _db.Badges.Where(b => b.IsActive).ToListAsync(ct);

    public Task<Badge?> GetByCodeAsync(string code, CancellationToken ct = default)
        => _db.Badges.FirstOrDefaultAsync(b => b.Code == code, ct);
}

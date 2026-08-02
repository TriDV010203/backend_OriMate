using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class CreatorVipSettingsRepository : ICreatorVipSettingsRepository
{
    private readonly AppDbContext _db;

    public CreatorVipSettingsRepository(AppDbContext db) => _db = db;

    public Task<CreatorVipSettings?> GetByCreatorIdAsync(Guid creatorId, CancellationToken ct = default)
        => _db.CreatorVipSettings.FirstOrDefaultAsync(s => s.CreatorId == creatorId, ct);

    public async Task AddAsync(CreatorVipSettings settings, CancellationToken ct = default)
    {
        _db.CreatorVipSettings.Add(settings);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(CreatorVipSettings settings, CancellationToken ct = default)
    {
        _db.CreatorVipSettings.Update(settings);
        await _db.SaveChangesAsync(ct);
    }
}

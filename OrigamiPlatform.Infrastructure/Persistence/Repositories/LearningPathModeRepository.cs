using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class LearningPathModeRepository : ILearningPathModeRepository
{
    private readonly AppDbContext _db;

    public LearningPathModeRepository(AppDbContext db) => _db = db;

    public async Task<List<LearningPathMode>> GetAllAsync(bool includeInactive, CancellationToken ct = default)
    {
        var query = _db.LearningPathModes.AsQueryable();
        if (!includeInactive)
            query = query.Where(m => m.IsActive);

        return await query.OrderBy(m => m.SortOrder).ToListAsync(ct);
    }

    public Task<LearningPathMode?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.LearningPathModes.FirstOrDefaultAsync(m => m.Id == id, ct);

    public Task<bool> ExistsBySortOrderAsync(int sortOrder, Guid? excludeId, CancellationToken ct = default)
        => _db.LearningPathModes.AnyAsync(
            m => m.SortOrder == sortOrder && (excludeId == null || m.Id != excludeId), ct);

    public Task<LearningPathMode?> GetImmediatePredecessorAsync(int sortOrder, CancellationToken ct = default)
        => _db.LearningPathModes
            .Where(m => m.IsActive && m.SortOrder < sortOrder)
            .OrderByDescending(m => m.SortOrder)
            .FirstOrDefaultAsync(ct);

    public Task<int> CountPathsAsync(Guid modeId, CancellationToken ct = default)
        => _db.LearningPaths.CountAsync(lp => lp.LearningPathModeId == modeId, ct);

    public async Task AddAsync(LearningPathMode mode, CancellationToken ct = default)
    {
        _db.LearningPathModes.Add(mode);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(LearningPathMode mode, CancellationToken ct = default)
    {
        _db.LearningPathModes.Update(mode);
        await _db.SaveChangesAsync(ct);
    }
}

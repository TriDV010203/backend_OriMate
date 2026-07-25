using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class LearningPathRepository : ILearningPathRepository
{
    private readonly AppDbContext _db;

    public LearningPathRepository(AppDbContext db) => _db = db;

    private IQueryable<LearningPath> BaseQuery() => _db.LearningPaths
        .Include(lp => lp.Items.OrderBy(i => i.ItemOrder))
            .ThenInclude(i => i.Tutorial).ThenInclude(t => t.Category)
        .AsSplitQuery();

    public async Task<PagedResult<LearningPath>> GetPublishedAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = BaseQuery().Where(lp => lp.Status == LearningPathStatus.Published);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(lp => lp.Title.Contains(search) || lp.Description.Contains(search));

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(lp => lp.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<LearningPath>(
            items, totalCount, page, pageSize, (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    public Task<LearningPath?> GetPublishedByIdAsync(Guid id, CancellationToken ct = default)
        => BaseQuery().FirstOrDefaultAsync(lp => lp.Id == id && lp.Status == LearningPathStatus.Published, ct);

    public Task<LearningPath?> GetPublishedPathContainingTutorialAsync(Guid tutorialId, CancellationToken ct = default)
        => BaseQuery()
            .Where(lp => lp.Status == LearningPathStatus.Published && lp.Items.Any(i => i.TutorialId == tutorialId))
            .OrderBy(lp => lp.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<PagedResult<LearningPath>> GetAllForAdminAsync(
        string? search, LearningPathStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = BaseQuery().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(lp => lp.Title.Contains(search) || lp.Description.Contains(search));

        if (status.HasValue)
            query = query.Where(lp => lp.Status == status.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(lp => lp.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<LearningPath>(
            items, totalCount, page, pageSize, (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    public Task<LearningPath?> GetByIdForAdminAsync(Guid id, CancellationToken ct = default)
        => BaseQuery().FirstOrDefaultAsync(lp => lp.Id == id, ct);

    public async Task AddAsync(LearningPath learningPath, CancellationToken ct = default)
    {
        _db.LearningPaths.Add(learningPath);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(LearningPath learningPath, CancellationToken ct = default)
    {
        _db.LearningPaths.Update(learningPath);
        await _db.SaveChangesAsync(ct);
    }

    public async Task ReplaceItemsAsync(
        Guid learningPathId, IEnumerable<LearningPathItem> items, CancellationToken ct = default)
    {
        // Same tracked-entity swap pattern as ITutorialRepository.DeleteStepsByTutorialIdAsync —
        // remove whatever the change tracker already holds for this path, then insert the new set,
        // avoiding stale-tracked-entity conflicts from a raw bulk delete.
        var trackedItems = _db.ChangeTracker.Entries<LearningPathItem>()
            .Where(e => e.Entity.LearningPathId == learningPathId)
            .Select(e => e.Entity)
            .ToList();

        if (trackedItems.Count > 0)
        {
            _db.LearningPathItems.RemoveRange(trackedItems);
            await _db.SaveChangesAsync(ct);
        }

        await _db.LearningPathItems
            .Where(i => i.LearningPathId == learningPathId)
            .ExecuteDeleteAsync(ct);

        _db.LearningPathItems.AddRange(items);
        await _db.SaveChangesAsync(ct);
    }
}

using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class TutorialRepository : ITutorialRepository
{
    private readonly AppDbContext _db;

    public TutorialRepository(AppDbContext db) => _db = db;

    // ── Public browsing ──────────────────────────────────────────────────────

    public async Task<(IEnumerable<Tutorial> Items, int TotalCount)> GetPublishedAsync(
        string? search,
        int? categoryId,
        TutorialDifficulty? difficulty,
        TutorialType? type,
        string sortBy,
        int page,
        int pageSize,
        IReadOnlySet<Guid>? followedCreatorIds = null,
        CancellationToken ct = default)
    {
        var query = _db.Tutorials
            .Where(t => t.Status == TutorialStatus.Published && !t.IsDeleted)
            .Include(t => t.Category)
            .Include(t => t.Author).ThenInclude(a => a.Profile)
            .Include(t => t.Steps)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Title.Contains(search) || t.Description.Contains(search));

        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        if (difficulty.HasValue)
            query = query.Where(t => t.Difficulty == difficulty.Value);

        if (type.HasValue)
            query = query.Where(t => t.Type == type.Value);

        var totalCount = await query.CountAsync(ct);

        IOrderedQueryable<Tutorial> ordered;
        if (sortBy == "likes")
        {
            ordered = query.OrderByDescending(t => _db.Likes.Count(l =>
                l.TargetType == TargetType.Tutorial && l.TargetId == t.Id));
        }
        else
        {
            var boostedIds = followedCreatorIds?.ToList() ?? new List<Guid>();
            ordered = boostedIds.Count > 0
                ? query
                    .OrderByDescending(t => boostedIds.Contains(t.AuthorId))
                    .ThenByDescending(t => t.PublishedAt)
                : query.OrderByDescending(t => t.PublishedAt);
        }

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public Task<Tutorial?> GetPublishedBySlugAsync(string slug, CancellationToken ct = default)
        => _db.Tutorials
            .Where(t => t.Slug == slug && t.Status == TutorialStatus.Published && !t.IsDeleted)
            .Include(t => t.Category)
            .Include(t => t.Author).ThenInclude(a => a.Profile)
            .Include(t => t.Steps)
            .AsSplitQuery()
            .FirstOrDefaultAsync(ct);

    // ── FT-04 authoring & review ─────────────────────────────────────────────

    public Task<Tutorial?> GetByIdWithStepsAsync(Guid id, CancellationToken ct = default)
        => _db.Tutorials
            .Where(t => t.Id == id && !t.IsDeleted)
            .Include(t => t.Steps)
            .Include(t => t.Author).ThenInclude(a => a.Profile)
            .AsSplitQuery()
            .FirstOrDefaultAsync(ct);

    public async Task<PagedResult<Tutorial>> GetByAuthorAsync(
        Guid authorId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Tutorials
            .Where(t => t.AuthorId == authorId && !t.IsDeleted)
            .Include(t => t.Steps)
            .AsQueryable();

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Tutorial>(
            items,
            totalCount,
            page,
            pageSize,
            (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    public async Task AddAsync(Tutorial tutorial, CancellationToken ct = default)
    {
        _db.Tutorials.Add(tutorial);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Tutorial tutorial, CancellationToken ct = default)
    {
        _db.Tutorials.Update(tutorial);
        await _db.SaveChangesAsync(ct);
    }

    // BR-17: never call Update/Delete on TutorialReviewHistory
    public async Task AddReviewHistoryAsync(TutorialReviewHistory history, CancellationToken ct = default)
    {
        _db.TutorialReviewHistories.Add(history);
        await _db.SaveChangesAsync(ct);
    }

    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default)
        => _db.Tutorials.AnyAsync(t => t.Slug == slug, ct);

    public Task<Category?> GetActiveCategoryAsync(int categoryId, CancellationToken ct = default)
        => _db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId && c.IsActive && !c.IsDeleted, ct);

    public Task<CreatorVipSettings?> GetActiveCreatorVipSettingsAsync(Guid creatorId, CancellationToken ct = default)
        => _db.CreatorVipSettings.FirstOrDefaultAsync(v => v.CreatorId == creatorId && v.IsActive, ct);

    // ── FT-07 Edit-after-publish ─────────────────────────────────────────────

    public Task<Tutorial?> GetWorkingCopyByParentIdAsync(Guid parentId, CancellationToken ct = default)
        => _db.Tutorials
            .Where(t => t.ParentTutorialId == parentId
                     && t.Status != TutorialStatus.Merged
                     && !t.IsDeleted)
            .Include(t => t.Steps)
            .FirstOrDefaultAsync(ct);

    public async Task DeleteStepsByTutorialIdAsync(Guid tutorialId, CancellationToken ct = default)
    {
        await _db.TutorialSteps
            .Where(s => s.TutorialId == tutorialId)
            .ExecuteDeleteAsync(ct);
    }

    public async Task AddStepsAsync(IEnumerable<TutorialStep> steps, CancellationToken ct = default)
    {
        _db.TutorialSteps.AddRange(steps);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<Tutorial>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        return await _db.Tutorials
            .Where(t => ids.Contains(t.Id))
            .ToListAsync(ct);
    }
}

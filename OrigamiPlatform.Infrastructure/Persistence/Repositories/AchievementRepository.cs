using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class AchievementRepository : IAchievementRepository
{
    private readonly AppDbContext _db;

    public AchievementRepository(AppDbContext db) => _db = db;

    public Task<Achievement?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Achievements
            .Include(a => a.Tutorial)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<Achievement?> GetByUserAndTutorialAsync(
        Guid userId,
        Guid tutorialId,
        CancellationToken ct = default)
        => _db.Achievements
            .Include(a => a.Tutorial)
            .FirstOrDefaultAsync(a => a.UserId == userId && a.TutorialId == tutorialId, ct);

    public async Task<(IEnumerable<Achievement> Items, int TotalCount)> GetByUserAsync(
        Guid userId,
        bool includePrivate,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.Achievements
            .Where(a => a.UserId == userId)
            .Include(a => a.Tutorial)
            .AsQueryable();

        if (!includePrivate)
            query = query.Where(a => a.IsPublic);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public Task<bool> PublishedTutorialExistsAsync(Guid tutorialId, CancellationToken ct = default)
        => _db.Tutorials.AnyAsync(
            t => t.Id == tutorialId
                && t.Status == TutorialStatus.Published
                && !t.IsDeleted,
            ct);

    public Task<int> CountByUserAsync(Guid userId, CancellationToken ct = default)
        => _db.Achievements.CountAsync(a => a.UserId == userId, ct);

    public Task<int> CountByTutorialAsync(Guid tutorialId, CancellationToken ct = default)
        => _db.Achievements.CountAsync(a => a.TutorialId == tutorialId, ct);

    public Task<int> CountByUserAndDifficultyAsync(Guid userId, TutorialDifficulty difficulty, CancellationToken ct = default)
        => _db.Achievements.CountAsync(a => a.UserId == userId && a.Tutorial.Difficulty == difficulty, ct);

    public async Task<HashSet<Guid>> GetCompletedTutorialIdsAsync(Guid userId, CancellationToken ct = default)
        => (await _db.Achievements
            .Where(a => a.UserId == userId)
            .Select(a => a.TutorialId)
            .ToListAsync(ct))
            .ToHashSet();

    public async Task<List<int>> GetCompletedCategoryIdsAsync(Guid userId, CancellationToken ct = default)
        => await _db.Achievements
            .Where(a => a.UserId == userId)
            .Select(a => a.Tutorial.CategoryId)
            .Distinct()
            .ToListAsync(ct);

    public async Task AddAsync(Achievement achievement, CancellationToken ct = default)
    {
        _db.Achievements.Add(achievement);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Concurrent request already created the (UserId, TutorialId) achievement — translate the
            // unique-index violation into the same domain error the pre-check above would have thrown.
            _db.Entry(achievement).State = EntityState.Detached;
            throw new DomainException("You already marked this tutorial as completed.");
        }
    }

    public async Task UpdateAsync(Achievement achievement, CancellationToken ct = default)
    {
        _db.Achievements.Update(achievement);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Achievement achievement, CancellationToken ct = default)
    {
        _db.Achievements.Remove(achievement);
        await _db.SaveChangesAsync(ct);
    }
}

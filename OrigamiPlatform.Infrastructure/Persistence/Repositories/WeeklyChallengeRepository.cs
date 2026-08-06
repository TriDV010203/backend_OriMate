using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class WeeklyChallengeRepository : IWeeklyChallengeRepository
{
    private readonly AppDbContext _db;

    public WeeklyChallengeRepository(AppDbContext db) => _db = db;

    public Task<WeeklyChallenge?> GetByDateAsync(DateOnly challengeDate, CancellationToken ct = default)
        => _db.WeeklyChallenges
            .Include(c => c.Tutorial).ThenInclude(t => t.Author).ThenInclude(a => a.Profile)
            .FirstOrDefaultAsync(c => c.ChallengeDate == challengeDate, ct);

    public Task<WeeklyChallenge?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.WeeklyChallenges
            .Include(c => c.Tutorial).ThenInclude(t => t.Author).ThenInclude(a => a.Profile)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddAsync(WeeklyChallenge challenge, CancellationToken ct = default)
    {
        _db.WeeklyChallenges.Add(challenge);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(WeeklyChallenge challenge, CancellationToken ct = default)
    {
        _db.WeeklyChallenges.Update(challenge);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<HashSet<Guid>> GetRecentlyUsedTutorialIdsAsync(DateOnly sinceDate, CancellationToken ct = default)
        => (await _db.WeeklyChallenges
            .Where(c => c.ChallengeDate >= sinceDate)
            .Select(c => c.TutorialId)
            .ToListAsync(ct))
            .ToHashSet();

    public async Task<List<(Tutorial Tutorial, int AchievementCount)>> GetEligibleCandidatesAsync(
        IReadOnlySet<Guid> excludeTutorialIds,
        TutorialDifficulty? difficulty,
        CancellationToken ct = default)
    {
        var query = _db.Tutorials
            .Where(t => t.Status == TutorialStatus.Published && !t.IsDeleted)
            .Where(t => !excludeTutorialIds.Contains(t.Id));

        if (difficulty.HasValue)
            query = query.Where(t => t.Difficulty == difficulty.Value);

        var candidates = await query
            .Select(t => new { Tutorial = t, AchievementCount = t.Achievements.Count })
            .ToListAsync(ct);

        return candidates.Select(c => (c.Tutorial, c.AchievementCount)).ToList();
    }

    public async Task<PagedResult<WeeklyChallenge>> GetAllForAdminAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        DailyChallengeStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.WeeklyChallenges
            .Include(c => c.Tutorial)
            .AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(c => c.ChallengeDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(c => c.ChallengeDate <= toDate.Value);
        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(c => c.ChallengeDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResult<WeeklyChallenge>(items, totalCount, page, pageSize, totalPages);
    }

    public Task<int> CountByTutorialAuthorAsync(Guid authorUserId, CancellationToken ct = default)
        => _db.WeeklyChallenges.CountAsync(c => c.Tutorial.AuthorId == authorUserId, ct);
}

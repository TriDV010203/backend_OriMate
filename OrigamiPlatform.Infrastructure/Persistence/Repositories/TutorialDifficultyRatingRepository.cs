using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class TutorialDifficultyRatingRepository : ITutorialDifficultyRatingRepository
{
    private readonly AppDbContext _db;

    public TutorialDifficultyRatingRepository(AppDbContext db) => _db = db;

    public Task<bool> ExistsAsync(Guid userId, Guid tutorialId, CancellationToken ct = default)
        => _db.TutorialDifficultyRatings.AnyAsync(r => r.UserId == userId && r.TutorialId == tutorialId, ct);

    public async Task AddAsync(TutorialDifficultyRating rating, CancellationToken ct = default)
    {
        _db.TutorialDifficultyRatings.Add(rating);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Concurrent request already rated this (UserId, TutorialId) pair — unique index
            // guarantees only one row exists; a duplicate rating isn't worth failing the request over.
            _db.Entry(rating).State = EntityState.Detached;
        }
    }

    public async Task<Dictionary<PerceivedDifficulty, int>> GetCountsAsync(Guid tutorialId, CancellationToken ct = default)
        => await _db.TutorialDifficultyRatings
            .Where(r => r.TutorialId == tutorialId)
            .GroupBy(r => r.Rating)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);
}

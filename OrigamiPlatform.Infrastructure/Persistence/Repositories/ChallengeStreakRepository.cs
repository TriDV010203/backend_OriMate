using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class ChallengeStreakRepository : IChallengeStreakRepository
{
    private readonly AppDbContext _db;

    public ChallengeStreakRepository(AppDbContext db) => _db = db;

    public async Task<ChallengeStreakLog> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var existing = await _db.ChallengeStreakLogs.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (existing is not null)
            return existing;

        var created = new ChallengeStreakLog { UserId = userId };
        _db.ChallengeStreakLogs.Add(created);
        await _db.SaveChangesAsync(ct);
        return created;
    }

    public async Task UpdateAsync(ChallengeStreakLog streak, CancellationToken ct = default)
    {
        _db.ChallengeStreakLogs.Update(streak);
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<ChallengeStreakLog>> GetTopAsync(int count, CancellationToken ct = default)
        => _db.ChallengeStreakLogs
            .Include(s => s.User).ThenInclude(u => u.Profile)
            .Where(s => s.CurrentStreak > 0)
            .OrderByDescending(s => s.CurrentStreak)
            .ThenByDescending(s => s.LongestStreak)
            .Take(count)
            .ToListAsync(ct);
}

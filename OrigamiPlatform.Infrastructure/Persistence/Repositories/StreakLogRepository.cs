using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class StreakLogRepository : IStreakLogRepository
{
    private readonly AppDbContext _db;

    public StreakLogRepository(AppDbContext db) => _db = db;

    public async Task<StreakLog> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var existing = await _db.StreakLogs.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (existing is not null)
            return existing;

        var created = new StreakLog { UserId = userId };
        _db.StreakLogs.Add(created);
        await _db.SaveChangesAsync(ct);
        return created;
    }

    public async Task UpdateAsync(StreakLog streakLog, CancellationToken ct = default)
    {
        _db.StreakLogs.Update(streakLog);
        await _db.SaveChangesAsync(ct);
    }
}

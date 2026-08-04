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

    public async Task<List<(Guid UserId, string Email)>> GetUsersInactiveForDaysAsync(int days, CancellationToken ct = default)
    {
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)).AddDays(-days);

        var rows = await _db.StreakLogs
            .Where(s => s.LastActiveDate == targetDate)
            .Join(_db.Users, s => s.UserId, u => u.Id, (s, u) => new { u.Id, u.Email })
            .ToListAsync(ct);

        return rows.Select(r => (r.Id, r.Email)).ToList();
    }
}

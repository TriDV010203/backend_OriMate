using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class DailyQuestRepository : IDailyQuestRepository
{
    private readonly AppDbContext _db;

    public DailyQuestRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<DailyQuest>> GetActiveAsync(CancellationToken ct = default)
        => await _db.DailyQuests.Where(q => q.IsActive).ToListAsync(ct);
}

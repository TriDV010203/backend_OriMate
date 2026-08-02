using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class StuckThreadRepository : IStuckThreadRepository
{
    private readonly AppDbContext _db;

    public StuckThreadRepository(AppDbContext db) => _db = db;

    public Task<StuckThread?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.StuckThreads.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<StuckThread?> GetByUserAndStepAsync(Guid userId, Guid stepId, CancellationToken ct = default)
        => _db.StuckThreads.FirstOrDefaultAsync(t => t.UserId == userId && t.StepId == stepId, ct);

    public async Task AddAsync(StuckThread thread, CancellationToken ct = default)
    {
        _db.StuckThreads.Add(thread);
        await _db.SaveChangesAsync(ct);
    }
}

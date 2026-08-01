using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class PaperPatternRepository : IPaperPatternRepository
{
    private readonly AppDbContext _db;

    public PaperPatternRepository(AppDbContext db) => _db = db;

    public Task<List<PaperPattern>> GetActiveAsync(CancellationToken ct = default)
        => _db.PaperPatterns.Where(p => p.IsActive).ToListAsync(ct);

    public Task<PaperPattern?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.PaperPatterns.FirstOrDefaultAsync(p => p.Id == id, ct);
}

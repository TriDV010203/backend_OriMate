using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class JournalRepository : IJournalRepository
{
    private readonly AppDbContext _db;

    public JournalRepository(AppDbContext db) => _db = db;

    public Task<Journal?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Journals
            .Include(j => j.LinkedTutorial)
            .FirstOrDefaultAsync(j => j.Id == id, ct);

    public async Task<(IEnumerable<Journal> Items, int TotalCount)> GetByUserAsync(
        Guid userId,
        bool includePrivate,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.Journals
            .Where(j => j.UserId == userId)
            .Include(j => j.LinkedTutorial)
            .AsQueryable();

        if (!includePrivate)
            query = query.Where(j => j.IsPublic);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(j => j.CreatedAt)
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

    public async Task AddAsync(Journal journal, CancellationToken ct = default)
    {
        _db.Journals.Add(journal);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Journal journal, CancellationToken ct = default)
    {
        _db.Journals.Update(journal);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Journal journal, CancellationToken ct = default)
    {
        _db.Journals.Remove(journal);
        await _db.SaveChangesAsync(ct);
    }
}

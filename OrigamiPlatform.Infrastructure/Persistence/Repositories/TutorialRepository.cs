using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class TutorialRepository : ITutorialRepository
{
    private readonly AppDbContext _db;

    public TutorialRepository(AppDbContext db) => _db = db;

    public async Task<(IEnumerable<Tutorial> Items, int TotalCount)> GetPublishedAsync(
        string? search,
        int? categoryId,
        string? difficulty,
        TutorialType? type,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.Tutorials
            .Where(t => t.Status == TutorialStatus.Published && !t.IsDeleted)
            .Include(t => t.Category)
            .Include(t => t.Author).ThenInclude(a => a.Profile)
            .Include(t => t.Steps)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Title.Contains(search) || t.Description.Contains(search));

        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(difficulty))
            query = query.Where(t => t.Difficulty == difficulty);

        if (type.HasValue)
            query = query.Where(t => t.Type == type.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public Task<Tutorial?> GetPublishedBySlugAsync(string slug, CancellationToken ct = default)
        => _db.Tutorials
            .Where(t => t.Slug == slug && t.Status == TutorialStatus.Published && !t.IsDeleted)
            .Include(t => t.Category)
            .Include(t => t.Author).ThenInclude(a => a.Profile)
            .Include(t => t.Steps)
            .AsSplitQuery()
            .FirstOrDefaultAsync(ct);
}

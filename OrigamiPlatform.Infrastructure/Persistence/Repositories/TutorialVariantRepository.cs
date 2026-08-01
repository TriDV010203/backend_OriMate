using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class TutorialVariantRepository : ITutorialVariantRepository
{
    private readonly AppDbContext _db;

    public TutorialVariantRepository(AppDbContext db) => _db = db;

    public Task<List<TutorialVariant>> GetByParentIdAsync(Guid parentTutorialId, CancellationToken ct = default)
        => _db.TutorialVariants
            .Where(v => v.ParentTutorialId == parentTutorialId)
            .Include(v => v.VariantTutorial)
            .ToListAsync(ct);

    public async Task AddAsync(TutorialVariant variant, CancellationToken ct = default)
    {
        _db.TutorialVariants.Add(variant);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(TutorialVariant variant, CancellationToken ct = default)
    {
        _db.TutorialVariants.Remove(variant);
        await _db.SaveChangesAsync(ct);
    }

    public Task<TutorialVariant?> GetByPairAsync(Guid parentId, Guid variantId, CancellationToken ct = default)
        => _db.TutorialVariants
            .FirstOrDefaultAsync(v => v.ParentTutorialId == parentId && v.VariantTutorialId == variantId, ct);
}

using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class ShopLinkRepository : IShopLinkRepository
{
    private readonly AppDbContext _db;

    public ShopLinkRepository(AppDbContext db) => _db = db;

    public Task<ShopLink?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.ShopLinks.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<List<ShopLink>> GetActiveAsync(CancellationToken ct = default)
        => _db.ShopLinks.Where(s => s.IsActive).OrderByDescending(s => s.CreatedAt).ToListAsync(ct);

    public async Task AddAsync(ShopLink link, CancellationToken ct = default)
    {
        _db.ShopLinks.Add(link);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ShopLink link, CancellationToken ct = default)
    {
        _db.ShopLinks.Update(link);
        await _db.SaveChangesAsync(ct);
    }
}

using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _db;

    public CategoryRepository(AppDbContext db) => _db = db;

    public Task<List<Category>> GetAllAsync(CancellationToken ct = default)
        => _db.Categories.Where(c => !c.IsDeleted).OrderByDescending(c => c.CreatedAt).ToListAsync(ct);

    public Task<Category?> GetByIdAsync(int id, CancellationToken ct = default)
        => _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default)
        => _db.Categories.AnyAsync(c =>
            !c.IsDeleted && c.Name.ToLower() == name.Trim().ToLower() && (excludeId == null || c.Id != excludeId), ct);

    public async Task<Category> AddAsync(Category category, CancellationToken ct = default)
    {
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(ct);
        return category;
    }

    public async Task UpdateAsync(Category category, CancellationToken ct = default)
    {
        _db.Categories.Update(category);
        await _db.SaveChangesAsync(ct);
    }
}

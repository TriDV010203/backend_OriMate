using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class ClanRepository : IClanRepository
{
    private readonly AppDbContext _db;

    public ClanRepository(AppDbContext db) => _db = db;

    public Task<Clan?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Clans.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Clan?> GetByNameAsync(string name, CancellationToken ct = default)
        => _db.Clans.FirstOrDefaultAsync(c => c.Name == name, ct);

    public async Task AddAsync(Clan clan, CancellationToken ct = default)
    {
        _db.Clans.Add(clan);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Clan clan, CancellationToken ct = default)
    {
        _db.Clans.Update(clan);
        await _db.SaveChangesAsync(ct);
    }
}

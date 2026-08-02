using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class ClanInviteRepository : IClanInviteRepository
{
    private readonly AppDbContext _db;

    public ClanInviteRepository(AppDbContext db) => _db = db;

    public Task<ClanInvite?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.ClanInvites
            .Include(i => i.Clan)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<List<ClanInvite>> GetPendingByUserIdAsync(Guid userId, CancellationToken ct = default)
        => _db.ClanInvites
            .Include(i => i.Clan)
            .Where(i => i.UserId == userId && i.Status == ClanInviteStatus.Pending)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(ClanInvite invite, CancellationToken ct = default)
    {
        _db.ClanInvites.Add(invite);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ClanInvite invite, CancellationToken ct = default)
    {
        _db.ClanInvites.Update(invite);
        await _db.SaveChangesAsync(ct);
    }
}

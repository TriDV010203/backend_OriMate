using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class ClanMemberRepository : IClanMemberRepository
{
    private readonly AppDbContext _db;

    public ClanMemberRepository(AppDbContext db) => _db = db;

    public Task<ClanMember?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => _db.ClanMembers
            .Include(m => m.Clan)
            .FirstOrDefaultAsync(m => m.UserId == userId, ct);

    public Task<List<ClanMember>> GetByClanIdAsync(Guid clanId, CancellationToken ct = default)
        => _db.ClanMembers
            .Include(m => m.User).ThenInclude(u => u.Profile)
            .Where(m => m.ClanId == clanId)
            .OrderBy(m => m.JoinedAt)
            .ToListAsync(ct);

    public async Task AddAsync(ClanMember member, CancellationToken ct = default)
    {
        _db.ClanMembers.Add(member);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(ClanMember member, CancellationToken ct = default)
    {
        _db.ClanMembers.Remove(member);
        await _db.SaveChangesAsync(ct);
    }
}

using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class FollowRepository : IFollowRepository
{
    private readonly AppDbContext _context;

    public FollowRepository(AppDbContext context) => _context = context;

    public async Task<FollowRelationship?> GetFollowAsync(Guid followerId, Guid followingId, CancellationToken ct = default)
    {
        return await _context.FollowRelationships
            .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId, ct);
    }

    public async Task AddAsync(FollowRelationship follow, CancellationToken ct = default)
    {
        await _context.FollowRelationships.AddAsync(follow, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(FollowRelationship follow, CancellationToken ct = default)
    {
        _context.FollowRelationships.Remove(follow);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<int> GetFollowersCountAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.FollowRelationships.CountAsync(f => f.FollowingId == userId, ct);
    }

    public async Task<int> GetFollowingCountAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.FollowRelationships.CountAsync(f => f.FollowerId == userId, ct);
    }
}
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.Common;
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

    public async Task<List<Guid>> GetFollowingIdsAsync(Guid followerId, CancellationToken ct = default)
    {
        return await _context.FollowRelationships
            .Where(f => f.FollowerId == followerId)
            .Select(f => f.FollowingId)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<User>> GetFollowersAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.FollowRelationships
            .Where(f => f.FollowingId == userId)
            .Include(f => f.Follower).ThenInclude(u => u.Profile)
            .Include(f => f.Follower).ThenInclude(u => u.Roles)
            .OrderByDescending(f => f.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => f.Follower)
            .ToListAsync(ct);

        return new PagedResult<User>(items, totalCount, page, pageSize, totalPages);
    }

    public async Task<PagedResult<User>> GetFollowingAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.FollowRelationships
            .Where(f => f.FollowerId == userId)
            .Include(f => f.Following).ThenInclude(u => u.Profile)
            .Include(f => f.Following).ThenInclude(u => u.Roles)
            .OrderByDescending(f => f.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => f.Following)
            .ToListAsync(ct);

        return new PagedResult<User>(items, totalCount, page, pageSize, totalPages);
    }
}
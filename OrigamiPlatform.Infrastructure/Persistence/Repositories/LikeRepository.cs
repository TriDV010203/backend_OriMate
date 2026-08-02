using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories
{
    public class LikeRepository : ILikeRepository
    {
        private readonly AppDbContext _context;

        public LikeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Like?> GetLikeAsync(Guid userId, Guid targetId, TargetType targetType)
        {
            return await _context.Likes
                .FirstOrDefaultAsync(l => l.UserId == userId && l.TargetId == targetId && l.TargetType == targetType);
        }

        public async Task AddAsync(Like like)
        {
            await _context.Likes.AddAsync(like);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveAsync(Like like)
        {
            _context.Likes.Remove(like);
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetLikeCountAsync(Guid targetId, TargetType targetType)
        {
            return await _context.Likes
                .CountAsync(l => l.TargetId == targetId && l.TargetType == targetType);
        }

        public async Task<Dictionary<Guid, int>> GetCountsForTargetsAsync(IEnumerable<Guid> targetIds, TargetType targetType)
        {
            var ids = targetIds.ToList();
            var counts = await _context.Likes
                .Where(l => l.TargetType == targetType && ids.Contains(l.TargetId))
                .GroupBy(l => l.TargetId)
                .Select(g => new { TargetId = g.Key, Count = g.Count() })
                .ToListAsync();

            return counts.ToDictionary(c => c.TargetId, c => c.Count);
        }

        public async Task<HashSet<Guid>> GetLikedTargetIdsAsync(Guid userId, IEnumerable<Guid> targetIds, TargetType targetType)
        {
            var ids = targetIds.ToList();
            var liked = await _context.Likes
                .Where(l => l.UserId == userId && l.TargetType == targetType && ids.Contains(l.TargetId))
                .Select(l => l.TargetId)
                .ToListAsync();

            return liked.ToHashSet();
        }
    }
}
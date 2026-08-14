using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.CommunityPosts;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories
{
    public class CommunityPostRepository : ICommunityPostRepository
    {
        private readonly AppDbContext _context;

        public CommunityPostRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CommunityPost> AddAsync(CommunityPost post)
        {
            await _context.CommunityPosts.AddAsync(post);
            await _context.SaveChangesAsync();
            return post;
        }

        public async Task<CommunityPost?> GetByIdAsync(Guid id)
        {
            return await _context.CommunityPosts
                .Include(p => p.Media) // Kéo theo cả Media nếu có
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<CommunityPost>> GetApprovedPostsAsync(int skip, int take)
        {
            return await _context.CommunityPosts
                .Include(p => p.Media)
                .Include(p => p.Comments) 
                .Where(p => p.IsVisible && !p.IsDeleted) 
                .OrderByDescending(p => p.CreatedAt)     
                .Skip(skip).Take(take)
                .ToListAsync();
        }
        public async Task<int> GetPostCountByAuthorAsync(Guid authorId, CancellationToken ct = default)
        {
            return await _context.CommunityPosts
                .CountAsync(p => p.AuthorId == authorId && p.IsVisible && !p.IsDeleted, ct);
        }

        public async Task<List<CommunityPost>> GetCommunityFeedAsync(List<Guid> followedUserIds, int skip, int take)
        {
            var query = _context.CommunityPosts
                .Include(p => p.Media)
                .Include(p => p.Comments)
                .Where(p => p.IsVisible && !p.IsDeleted);

            if (followedUserIds != null && followedUserIds.Any())
            {
                query = query
                    .OrderByDescending(p => followedUserIds.Contains(p.AuthorId)) 
                    .ThenByDescending(p => p.CreatedAt); 
            }
            else
            {
                query = query.OrderByDescending(p => p.CreatedAt);
            }

            return await query.Skip(skip).Take(take).ToListAsync();
        }

        public async Task<List<CommunityPost>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
        {
            return await _context.CommunityPosts
                .Include(p => p.Media) // Phải Include Media thì Handler mới map sang MediaItemDto được
                .Where(p => ids.Contains(p.Id))
                .ToListAsync(ct);
        }

        public async Task<CommunityPost> UpdateAsync(CommunityPost post, CancellationToken ct = default)
        {
            _context.CommunityPosts.Update(post);
            await _context.SaveChangesAsync(ct);
            return post; // 🟢 Trả về post để khớp với chữ ký của Interface
        }

        public async Task<List<CommunityPostDto>> GetCommunityFeedListAsync(
            List<Guid> followedUserIds, Guid? currentUserId, int skip, int take, CancellationToken ct = default)
        {
            var query = _context.CommunityPosts
                .AsNoTracking()
                .Where(p => p.IsVisible && !p.IsDeleted);

            IOrderedQueryable<CommunityPost> ordered = followedUserIds.Count > 0
                ? query.OrderByDescending(p => followedUserIds.Contains(p.AuthorId)).ThenByDescending(p => p.CreatedAt)
                : query.OrderByDescending(p => p.CreatedAt);

            var page = ordered.Skip(skip).Take(take);

            if (currentUserId.HasValue)
            {
                var uid = currentUserId.Value;
                return await page.Select(post => new CommunityPostDto(
                    post.Id,
                    post.AuthorId,
                    post.Content,
                    post.CreatedAt,
                    _context.Comments.Count(c => !c.IsDeleted && (
                        (c.TargetType == TargetType.CommunityPost && c.TargetId == post.Id) ||
                        (c.TargetType == TargetType.Comment && _context.Comments.Any(p =>
                            p.Id == c.TargetId && p.TargetType == TargetType.CommunityPost && p.TargetId == post.Id && !p.IsDeleted)))),
                    _context.Likes.Count(l => l.TargetType == TargetType.CommunityPost && l.TargetId == post.Id),
                    _context.Likes.Any(l => l.UserId == uid && l.TargetType == TargetType.CommunityPost && l.TargetId == post.Id),
                    followedUserIds.Contains(post.AuthorId),
                    post.Media.OrderBy(m => m.DisplayOrder).Select(m => new MediaItemDto
                    {
                        MediaUrl = m.Url,
                        MediaType = m.MediaType
                    }).ToList()
                )).ToListAsync(ct);
            }

            return await page.Select(post => new CommunityPostDto(
                post.Id,
                post.AuthorId,
                post.Content,
                post.CreatedAt,
                _context.Comments.Count(c => !c.IsDeleted && (
                    (c.TargetType == TargetType.CommunityPost && c.TargetId == post.Id) ||
                    (c.TargetType == TargetType.Comment && _context.Comments.Any(p =>
                        p.Id == c.TargetId && p.TargetType == TargetType.CommunityPost && p.TargetId == post.Id && !p.IsDeleted)))),
                _context.Likes.Count(l => l.TargetType == TargetType.CommunityPost && l.TargetId == post.Id),
                false,
                followedUserIds.Contains(post.AuthorId),
                post.Media.OrderBy(m => m.DisplayOrder).Select(m => new MediaItemDto
                {
                    MediaUrl = m.Url,
                    MediaType = m.MediaType
                }).ToList()
            )).ToListAsync(ct);
        }
    }
}
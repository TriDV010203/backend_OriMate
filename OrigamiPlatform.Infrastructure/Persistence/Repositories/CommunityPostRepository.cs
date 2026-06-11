using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

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
                .Include(p => p.Comments) // Include để đếm comment
                .Where(p => p.IsVisible && !p.IsDeleted) // Chỉ lấy bài không bị ẩn/xóa
                .OrderByDescending(p => p.CreatedAt)     // AC-01: Sorted by newest
                .Skip(skip).Take(take)
                .ToListAsync();
        }
    }
}
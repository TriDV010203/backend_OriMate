using OrigamiPlatform.Application.Interfaces.Repositories;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Infrastructure.Persistence;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class CommunityRepository : ICommunityRepository
{
    private readonly AppDbContext _context;

    public CommunityRepository(AppDbContext context) => _context = context;

    public async Task AddPostAsync(CommunityPost post)
    {
        _context.CommunityPosts.Add(post);
        await _context.SaveChangesAsync();
    }

    public async Task<Like?> GetLikeAsync(Guid userId, Guid targetId, TargetType targetType)
    {
        return await _context.Likes
            .FirstOrDefaultAsync(l => l.UserId == userId && l.TargetId == targetId && l.TargetType == targetType);
    }

    public async Task AddLikeAsync(Like like)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Likes.Add(like);

            // Cập nhật Count (Ví dụ cho Post)
            if (like.TargetType == TargetType.Post)
            {
                var post = await _context.CommunityPosts.FindAsync(like.TargetId);
                if (post != null) post.LikeCount++;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch { await transaction.RollbackAsync(); throw; }
    }

    public async Task RemoveLikeAsync(Like like)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Likes.Remove(like);

            if (like.TargetType == TargetType.Post)
            {
                var post = await _context.CommunityPosts.FindAsync(like.TargetId);
                if (post != null && post.LikeCount > 0) post.LikeCount--;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch { await transaction.RollbackAsync(); throw; }
    }
}
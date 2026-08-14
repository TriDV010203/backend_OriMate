using OrigamiPlatform.Application.DTOs.CommunityPosts;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces
{
    public interface ICommunityPostRepository
    {
        Task<CommunityPost> AddAsync(CommunityPost post);
        Task<CommunityPost?> GetByIdAsync(Guid id);
        Task<List<CommunityPost>> GetApprovedPostsAsync(int skip, int take);
        Task<int> GetPostCountByAuthorAsync(Guid authorId, CancellationToken ct = default);
        Task<List<CommunityPost>> GetCommunityFeedAsync(List<Guid> followedUserIds, int skip, int take);
        Task<List<CommunityPost>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
        Task<CommunityPost> UpdateAsync(CommunityPost post, CancellationToken ct = default);

        // Single-query DTO projection (avoids N+1 like/comment/isLiked lookups)
        Task<List<CommunityPostDto>> GetCommunityFeedListAsync(
            List<Guid> followedUserIds, Guid? currentUserId, int skip, int take, CancellationToken ct = default);
    }
}
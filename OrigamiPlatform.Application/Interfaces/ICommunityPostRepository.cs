using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces
{
    public interface ICommunityPostRepository
    {
        Task<CommunityPost> AddAsync(CommunityPost post);
        Task<CommunityPost?> GetByIdAsync(Guid id);
        Task<List<CommunityPost>> GetApprovedPostsAsync(int skip, int take);
    }
}
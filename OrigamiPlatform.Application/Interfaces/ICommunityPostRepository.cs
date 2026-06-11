using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces
{
    public interface ICommunityPostRepository
    {
        Task<CommunityPost> AddAsync(CommunityPost post);
        Task<CommunityPost?> GetByIdAsync(Guid id);
        // Tạm thời chuẩn bị 2 hàm này, các hàm get list sẽ bổ sung sau khi làm Query
        Task<List<CommunityPost>> GetApprovedPostsAsync(int skip, int take);
    }
}
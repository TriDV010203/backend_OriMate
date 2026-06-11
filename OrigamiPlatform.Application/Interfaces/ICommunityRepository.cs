using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces.Repositories;

public interface ICommunityRepository
{
    Task AddPostAsync(CommunityPost post);
    Task<Like?> GetLikeAsync(Guid userId, Guid targetId, TargetType targetType);
    Task AddLikeAsync(Like like);
    Task RemoveLikeAsync(Like like);
}
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Interfaces
{
    public interface ILikeRepository
    {
        Task<Like?> GetLikeAsync(Guid userId, Guid targetId, TargetType targetType);
        Task AddAsync(Like like);
        Task RemoveAsync(Like like);
        Task<int> GetLikeCountAsync(Guid targetId, TargetType targetType);
    }
}
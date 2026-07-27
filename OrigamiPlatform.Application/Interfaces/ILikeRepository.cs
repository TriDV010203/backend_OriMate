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

        /// <summary>Bulk like-count lookup for a set of targets of the same type — avoids N+1 when ranking a list (e.g. Daily Challenge submissions).</summary>
        Task<Dictionary<Guid, int>> GetCountsForTargetsAsync(IEnumerable<Guid> targetIds, TargetType targetType);

        /// <summary>Which of the given targets the user has liked — avoids N+1 when rendering a list with an "isLiked" flag per item.</summary>
        Task<HashSet<Guid>> GetLikedTargetIdsAsync(Guid userId, IEnumerable<Guid> targetIds, TargetType targetType);
    }
}
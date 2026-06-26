using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IFollowRepository
{
    Task<FollowRelationship?> GetFollowAsync(Guid followerId, Guid followingId, CancellationToken ct = default);
    Task AddAsync(FollowRelationship follow, CancellationToken ct = default);
    Task RemoveAsync(FollowRelationship follow, CancellationToken ct = default);
    Task<int> GetFollowersCountAsync(Guid userId, CancellationToken ct = default);
    Task<int> GetFollowingCountAsync(Guid userId, CancellationToken ct = default);
}
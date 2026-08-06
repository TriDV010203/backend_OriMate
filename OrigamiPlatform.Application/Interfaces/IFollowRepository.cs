using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IFollowRepository
{
    Task<FollowRelationship?> GetFollowAsync(Guid followerId, Guid followingId, CancellationToken ct = default);
    Task AddAsync(FollowRelationship follow, CancellationToken ct = default);
    Task RemoveAsync(FollowRelationship follow, CancellationToken ct = default);
    Task<int> GetFollowersCountAsync(Guid userId, CancellationToken ct = default);
    Task<int> GetFollowingCountAsync(Guid userId, CancellationToken ct = default);
    Task<List<Guid>> GetFollowingIdsAsync(Guid followerId, CancellationToken ct = default);
    Task<PagedResult<User>> GetFollowersAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<User>> GetFollowingAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetTopFollowedUsersAsync(int count, CancellationToken ct = default);
}
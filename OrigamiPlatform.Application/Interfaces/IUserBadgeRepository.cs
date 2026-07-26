using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IUserBadgeRepository
{
    Task<bool> ExistsAsync(Guid userId, Guid badgeId, CancellationToken ct = default);
    Task AddAsync(UserBadge userBadge, CancellationToken ct = default);
    Task<List<UserBadge>> GetByUserAsync(Guid userId, CancellationToken ct = default);
}

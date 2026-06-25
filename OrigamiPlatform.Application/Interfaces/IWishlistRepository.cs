using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Interfaces;

public interface IWishlistRepository
{
    Task<Wishlist?> GetByUserAndTargetAsync(Guid userId, Guid targetId, TargetType targetType, CancellationToken ct = default);
    Task AddAsync(Wishlist wishlist, CancellationToken ct = default);
    Task RemoveAsync(Wishlist wishlist, CancellationToken ct = default);
    Task<PagedResult<Wishlist>> GetUserWishlistAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<int> GetWishlistCountAsync(Guid targetId, Domain.Enums.TargetType targetType, CancellationToken ct = default);
}
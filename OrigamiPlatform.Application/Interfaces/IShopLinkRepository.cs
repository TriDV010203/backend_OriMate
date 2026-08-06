using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IShopLinkRepository
{
    Task<ShopLink?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<ShopLink>> GetActiveAsync(CancellationToken ct = default);
    Task<List<ShopLink>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(ShopLink link, CancellationToken ct = default);
    Task UpdateAsync(ShopLink link, CancellationToken ct = default);
}

using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface ITutorialVariantRepository
{
    Task<List<TutorialVariant>> GetByParentIdAsync(Guid parentTutorialId, CancellationToken ct = default);
    Task AddAsync(TutorialVariant variant, CancellationToken ct = default);
    Task DeleteAsync(TutorialVariant variant, CancellationToken ct = default);
    Task<TutorialVariant?> GetByPairAsync(Guid parentId, Guid variantId, CancellationToken ct = default);
}

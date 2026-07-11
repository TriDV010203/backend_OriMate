using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface ICreatorVipSettingsRepository
{
    Task<CreatorVipSettings?> GetByCreatorIdAsync(Guid creatorId, CancellationToken ct = default);

    Task AddAsync(CreatorVipSettings settings, CancellationToken ct = default);

    Task UpdateAsync(CreatorVipSettings settings, CancellationToken ct = default);
}

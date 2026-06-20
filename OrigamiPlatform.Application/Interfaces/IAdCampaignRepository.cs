using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IAdCampaignRepository
{
    /// <summary>Returns the placement only when it exists and is active (BV/UC-10).</summary>
    Task<AdPlacement?> GetActivePlacementAsync(int placementId, CancellationToken ct = default);

    Task AddAsync(AdCampaign campaign, CancellationToken ct = default);
    Task<AdCampaign?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task UpdateAsync(AdCampaign campaign, CancellationToken ct = default);
}

using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Interfaces;

public interface IAdCampaignRepository
{
    /// <summary>Returns the placement only when it exists and is active (BV/UC-10).</summary>
    Task<AdPlacement?> GetActivePlacementAsync(int placementId, CancellationToken ct = default);

    Task AddAsync(AdCampaign campaign, CancellationToken ct = default);
    Task<AdCampaign?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task UpdateAsync(AdCampaign campaign, CancellationToken ct = default);

    Task<(IEnumerable<AdCampaign> Items, int TotalCount)> GetByPartnerAsync(
        Guid partnerId, int page, int pageSize, CancellationToken ct = default);
    Task<(IEnumerable<AdCampaign> Items, int TotalCount)> GetByStatusAsync(
        CampaignStatus status, int page, int pageSize, CancellationToken ct = default);

    // FT-21 tracking
    Task AddImpressionAsync(AdImpression impression, CancellationToken ct = default);
    Task AddClickAsync(AdClick click, CancellationToken ct = default);
    Task<int> CountImpressionsAsync(Guid campaignId, CancellationToken ct = default);
    Task<int> CountClicksAsync(Guid campaignId, CancellationToken ct = default);

    /// <summary>Campaigns whose banners may be displayed in a placement right now (live + budget &gt; 0).</summary>
    Task<IReadOnlyList<AdCampaign>> GetLiveByPlacementAsync(
        int placementId, DateTime today, CancellationToken ct = default);
}

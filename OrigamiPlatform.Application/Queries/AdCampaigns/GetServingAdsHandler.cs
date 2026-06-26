using OrigamiPlatform.Application.DTOs.AdCampaigns;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.AdCampaigns;

public class GetServingAdsHandler
{
    private readonly IAdCampaignRepository _campaigns;

    public GetServingAdsHandler(IAdCampaignRepository campaigns)
        => _campaigns = campaigns;

    public async Task<IReadOnlyList<ServedAdDto>> HandleAsync(
        GetServingAdsQuery query, CancellationToken ct = default)
    {
        // NAC-01: only live campaigns with budget remaining are returned for display.
        var live = await _campaigns.GetLiveByPlacementAsync(query.PlacementId, DateTime.UtcNow.Date, ct);

        return live
            .SelectMany(c => c.Banners.Select(b =>
                new ServedAdDto(c.Id, b.Id, b.ImageUrl, b.AltText, c.DestinationUrl)))
            .ToList();
    }
}

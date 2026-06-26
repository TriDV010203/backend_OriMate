using OrigamiPlatform.Application.DTOs.AdCampaigns;
using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.AdCampaigns;

public class GetMyCampaignsHandler
{
    private readonly IAdCampaignRepository _campaigns;

    public GetMyCampaignsHandler(IAdCampaignRepository campaigns)
        => _campaigns = campaigns;

    public async Task<PagedResult<AdCampaignDto>> HandleAsync(
        GetMyCampaignsQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var (items, total) = await _campaigns.GetByPartnerAsync(query.PartnerId, page, pageSize, ct);
        var dtos = items.Select(c => c.ToDto()).ToList();

        return new PagedResult<AdCampaignDto>(
            dtos, total, page, pageSize, (int)Math.Ceiling(total / (double)pageSize));
    }
}

using OrigamiPlatform.Application.DTOs.AdCampaigns;
using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Queries.AdCampaigns;

public class GetPendingCampaignsHandler
{
    private readonly IAdCampaignRepository _campaigns;

    public GetPendingCampaignsHandler(IAdCampaignRepository campaigns)
        => _campaigns = campaigns;

    public async Task<PagedResult<AdCampaignDto>> HandleAsync(
        GetPendingCampaignsQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var (items, total) = await _campaigns.GetByStatusAsync(
            CampaignStatus.PendingApproval, page, pageSize, ct);
        var dtos = items.Select(c => c.ToDto()).ToList();

        return new PagedResult<AdCampaignDto>(
            dtos, total, page, pageSize, (int)Math.Ceiling(total / (double)pageSize));
    }
}

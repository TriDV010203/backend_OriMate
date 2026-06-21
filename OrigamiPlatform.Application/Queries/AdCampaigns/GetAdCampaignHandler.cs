using OrigamiPlatform.Application.DTOs.AdCampaigns;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Queries.AdCampaigns;

public class GetAdCampaignHandler
{
    private readonly IAdCampaignRepository _campaigns;

    public GetAdCampaignHandler(IAdCampaignRepository campaigns)
        => _campaigns = campaigns;

    public async Task<AdCampaignDto> HandleAsync(GetAdCampaignQuery query, CancellationToken ct = default)
    {
        var campaign = await _campaigns.GetByIdAsync(query.CampaignId, ct)
            ?? throw new NotFoundException("Ad campaign not found.");

        // A partner may only view their own campaign; Managers/Admins may view any.
        if (!query.IsPrivileged && campaign.PartnerId != query.CurrentUserId)
            throw new ForbiddenException("You can only view your own campaigns.");

        return campaign.ToDto();
    }
}

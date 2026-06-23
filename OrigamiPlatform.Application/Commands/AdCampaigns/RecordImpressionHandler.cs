using OrigamiPlatform.Application.DTOs.AdCampaigns;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.AdCampaigns;

public class RecordImpressionHandler
{
    private readonly IAdCampaignRepository _campaigns;

    public RecordImpressionHandler(IAdCampaignRepository campaigns)
        => _campaigns = campaigns;

    public async Task<AdTrackResultDto> HandleAsync(RecordImpressionCommand command, CancellationToken ct = default)
    {
        var campaign = await _campaigns.GetByIdAsync(command.CampaignId, ct)
            ?? throw new NotFoundException("Ad campaign not found.");

        if (campaign.Banners.All(b => b.Id != command.BannerId))
            throw new NotFoundException("Banner not found in this campaign.");

        // NAC-01/02: only record while the campaign is live with budget remaining.
        if (!AdCampaignRules.IsLive(campaign, DateTime.UtcNow.Date))
            throw new DomainException("The campaign is not active; the impression was not recorded.");

        var now = DateTime.UtcNow;
        // CPM deducts per impression (rate per 1,000); CPC impressions are free.
        var cost = campaign.PricingType == PricingType.CPM ? campaign.RatePerUnit / 1000m : 0m;

        await _campaigns.AddImpressionAsync(new AdImpression
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            BannerId = command.BannerId,
            UserId = command.UserId,
            Cost = cost,
            OccurredAt = now
        }, ct);

        if (cost > 0)
        {
            AdCampaignRules.Deduct(campaign, cost, now);
            await _campaigns.UpdateAsync(campaign, ct);
        }

        return new AdTrackResultDto(campaign.Status.ToString(), campaign.BudgetRemaining);
    }
}

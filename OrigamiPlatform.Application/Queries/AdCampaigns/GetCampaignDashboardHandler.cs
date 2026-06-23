using OrigamiPlatform.Application.DTOs.AdCampaigns;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Queries.AdCampaigns;

public class GetCampaignDashboardHandler
{
    private readonly IAdCampaignRepository _campaigns;

    public GetCampaignDashboardHandler(IAdCampaignRepository campaigns)
        => _campaigns = campaigns;

    public async Task<AdCampaignDashboardDto> HandleAsync(
        GetCampaignDashboardQuery query, CancellationToken ct = default)
    {
        var c = await _campaigns.GetByIdAsync(query.CampaignId, ct)
            ?? throw new NotFoundException("Ad campaign not found.");

        if (!query.IsPrivileged && c.PartnerId != query.CurrentUserId)
            throw new ForbiddenException("You can only view your own campaign dashboard.");

        var impressions = await _campaigns.CountImpressionsAsync(c.Id, ct);
        var clicks = await _campaigns.CountClicksAsync(c.Id, ct);
        var ctr = impressions == 0 ? 0d : Math.Round((double)clicks / impressions, 4);
        var spent = c.TotalBudget - c.BudgetRemaining;

        // AC-04: impressions, clicks, CTR, budget spent/remaining, status.
        return new AdCampaignDashboardDto(
            c.Id, c.Name, c.Status.ToString(),
            impressions, clicks, ctr,
            c.TotalBudget, spent, c.BudgetRemaining);
    }
}

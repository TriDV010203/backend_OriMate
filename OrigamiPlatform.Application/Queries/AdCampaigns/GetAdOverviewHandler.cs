using OrigamiPlatform.Application.DTOs.AdCampaigns;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Queries.AdCampaigns;

public class GetAdOverviewHandler
{
    private readonly IAdCampaignRepository _campaigns;

    public GetAdOverviewHandler(IAdCampaignRepository campaigns)
        => _campaigns = campaigns;

    public async Task<AdPlatformOverviewDto> HandleAsync(
        GetAdOverviewQuery query, CancellationToken ct = default)
    {
        var all = await _campaigns.GetAllAsync(ct);
        var impressions = await _campaigns.CountAllImpressionsAsync(ct);
        var clicks = await _campaigns.CountAllClicksAsync(ct);

        var pending = all.Count(c => c.Status == CampaignStatus.PendingApproval);
        var running = all.Count(c => c.Status is CampaignStatus.Approved or CampaignStatus.Active);
        var ended = all.Count(c => c.Status == CampaignStatus.Ended);
        var spent = all.Sum(c => c.TotalBudget - c.BudgetRemaining);

        return new AdPlatformOverviewDto(
            all.Count, pending, running, ended, impressions, clicks, spent);
    }
}

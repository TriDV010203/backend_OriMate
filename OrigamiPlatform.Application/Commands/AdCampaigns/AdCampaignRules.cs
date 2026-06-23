using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Commands.AdCampaigns;

internal static class AdCampaignRules
{
    /// <summary>A campaign may be shown/charged when it is approved or running, in schedule, with budget left.</summary>
    public static bool IsLive(AdCampaign c, DateTime today)
        => (c.Status == CampaignStatus.Approved || c.Status == CampaignStatus.Active)
            && c.BudgetRemaining > 0
            && c.StartDate.Date <= today
            && c.EndDate.Date >= today;

    /// <summary>Deduct a cost; when the budget reaches zero the campaign ends (AC-03).</summary>
    public static void Deduct(AdCampaign c, decimal cost, DateTime now)
    {
        c.BudgetRemaining -= cost;
        if (c.BudgetRemaining <= 0)
        {
            c.BudgetRemaining = 0;
            c.Status = CampaignStatus.Ended;
        }
        c.UpdatedAt = now;
    }
}

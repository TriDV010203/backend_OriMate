using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.DTOs.AdCampaigns;

public static class AdCampaignMapping
{
    public static AdCampaignDto ToDto(this AdCampaign c)
        => new(
            c.Id,
            c.PartnerId,
            c.PlacementId,
            c.Placement?.Name,
            c.Name,
            c.DestinationUrl,
            c.PricingType.ToString(),
            c.RatePerUnit,
            c.TotalBudget,
            c.BudgetRemaining,
            c.Status.ToString(),
            c.StartDate,
            c.EndDate,
            c.RejectionReason,
            c.CreatedAt,
            c.Banners
                .Select(b => new AdBannerDto(b.Id, b.ImageUrl, b.AltText))
                .ToList());
}

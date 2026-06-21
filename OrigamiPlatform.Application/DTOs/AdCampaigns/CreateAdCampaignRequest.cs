namespace OrigamiPlatform.Application.DTOs.AdCampaigns;

public record CreateAdCampaignRequest(
    string Name,
    string DestinationUrl,
    int PlacementId,
    string PricingType,
    decimal RatePerUnit,
    decimal TotalBudget,
    DateTime StartDate,
    DateTime EndDate,
    string BannerImageUrl,
    string? AltText);

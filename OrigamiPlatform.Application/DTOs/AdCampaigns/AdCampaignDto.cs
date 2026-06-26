namespace OrigamiPlatform.Application.DTOs.AdCampaigns;

public record AdCampaignDto(
    Guid Id,
    Guid PartnerId,
    int PlacementId,
    string? PlacementName,
    string Name,
    string DestinationUrl,
    string PricingType,
    decimal RatePerUnit,
    decimal TotalBudget,
    decimal BudgetRemaining,
    string Status,
    DateTime StartDate,
    DateTime EndDate,
    string? RejectionReason,
    DateTime CreatedAt,
    IReadOnlyList<AdBannerDto> Banners);

public record AdBannerDto(Guid Id, string ImageUrl, string? AltText);

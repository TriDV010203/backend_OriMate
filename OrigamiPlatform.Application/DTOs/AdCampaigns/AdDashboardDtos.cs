namespace OrigamiPlatform.Application.DTOs.AdCampaigns;

public record ServedAdDto(
    Guid CampaignId,
    Guid BannerId,
    string ImageUrl,
    string? AltText,
    string DestinationUrl);

public record AdCampaignDashboardDto(
    Guid CampaignId,
    string Name,
    string Status,
    int TotalImpressions,
    int TotalClicks,
    double Ctr,
    decimal TotalBudget,
    decimal BudgetSpent,
    decimal BudgetRemaining);

public record AdPlatformOverviewDto(
    int TotalCampaigns,
    int Pending,
    int Running,
    int Ended,
    int TotalImpressions,
    int TotalClicks,
    decimal TotalSpent);

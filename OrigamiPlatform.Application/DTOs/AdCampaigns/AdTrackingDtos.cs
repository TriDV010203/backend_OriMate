namespace OrigamiPlatform.Application.DTOs.AdCampaigns;

public record TrackAdRequest(Guid CampaignId, Guid BannerId);

public record AdTrackResultDto(string Status, decimal BudgetRemaining);

public record AdClickResultDto(string DestinationUrl, string Status, decimal BudgetRemaining);

namespace OrigamiPlatform.Application.Queries.AdCampaigns;

public record GetAdCampaignQuery(Guid CampaignId, Guid CurrentUserId, bool IsPrivileged);

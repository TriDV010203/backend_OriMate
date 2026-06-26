namespace OrigamiPlatform.Application.Queries.AdCampaigns;

public record GetCampaignDashboardQuery(Guid CampaignId, Guid CurrentUserId, bool IsPrivileged);

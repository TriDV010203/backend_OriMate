namespace OrigamiPlatform.Application.Queries.AdCampaigns;

public record GetMyCampaignsQuery(Guid PartnerId, int Page, int PageSize);

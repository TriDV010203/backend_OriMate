namespace OrigamiPlatform.Application.Commands.AdCampaigns;

public record RecordImpressionCommand(Guid CampaignId, Guid BannerId, Guid? UserId);

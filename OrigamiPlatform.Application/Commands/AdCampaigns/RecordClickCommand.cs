namespace OrigamiPlatform.Application.Commands.AdCampaigns;

public record RecordClickCommand(Guid CampaignId, Guid BannerId, Guid? UserId);

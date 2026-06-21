namespace OrigamiPlatform.Application.Commands.AdCampaigns;

public record ReviewAdCampaignCommand(Guid CampaignId, Guid ManagerId, bool Approve, string? Reason);

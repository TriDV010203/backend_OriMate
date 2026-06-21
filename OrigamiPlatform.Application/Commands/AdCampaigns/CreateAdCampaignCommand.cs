using OrigamiPlatform.Application.DTOs.AdCampaigns;

namespace OrigamiPlatform.Application.Commands.AdCampaigns;

public record CreateAdCampaignCommand(Guid PartnerId, CreateAdCampaignRequest Request);

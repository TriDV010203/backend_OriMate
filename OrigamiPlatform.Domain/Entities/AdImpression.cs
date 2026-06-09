namespace OrigamiPlatform.Domain.Entities;

public class AdImpression
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Guid? UserId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }

    public AdCampaign Campaign { get; set; } = null!;
}

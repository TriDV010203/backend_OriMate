namespace OrigamiPlatform.Domain.Entities;

public class AdBanner
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? TargetUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public AdCampaign Campaign { get; set; } = null!;
}

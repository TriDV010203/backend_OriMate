namespace OrigamiPlatform.Domain.Entities;

public class AdBanner
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public AdCampaign Campaign { get; set; } = null!;
}

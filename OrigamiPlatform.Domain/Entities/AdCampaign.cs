using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Domain.Entities;

public class AdCampaign
{
    public Guid Id { get; set; }
    public Guid AdvertiserId { get; set; }
    public Guid PlacementId { get; set; }
    public string Name { get; set; } = string.Empty;
    public CampaignStatus Status { get; set; }
    public PricingType PricingType { get; set; }
    public decimal Budget { get; set; }
    public decimal BudgetRemaining { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User Advertiser { get; set; } = null!;
    public AdPlacement Placement { get; set; } = null!;
    public ICollection<AdBanner> Banners { get; set; } = new List<AdBanner>();
    public ICollection<AdImpression> Impressions { get; set; } = new List<AdImpression>();
    public ICollection<AdClick> Clicks { get; set; } = new List<AdClick>();
}

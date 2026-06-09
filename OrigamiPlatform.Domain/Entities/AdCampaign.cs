using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Domain.Entities;

public class AdCampaign
{
    public Guid Id { get; set; }
    public Guid PartnerId { get; set; }
    public int PlacementId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DestinationUrl { get; set; } = string.Empty;
    public PricingType PricingType { get; set; }
    public decimal RatePerUnit { get; set; }
    public decimal TotalBudget { get; set; }
    public decimal BudgetRemaining { get; set; }
    public CampaignStatus Status { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User Partner { get; set; } = null!;
    public AdPlacement Placement { get; set; } = null!;
    public User? ApprovedByUser { get; set; }
    public ICollection<AdBanner> Banners { get; set; } = new List<AdBanner>();
    public ICollection<AdImpression> Impressions { get; set; } = new List<AdImpression>();
    public ICollection<AdClick> Clicks { get; set; } = new List<AdClick>();
}

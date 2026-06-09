namespace OrigamiPlatform.Domain.Entities;

public class AdClick
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Guid BannerId { get; set; }
    public Guid? StepId { get; set; }
    public Guid? UserId { get; set; }
    public string? IpAddress { get; set; }
    public decimal Cost { get; set; }
    public DateTime OccurredAt { get; set; }

    public AdCampaign Campaign { get; set; } = null!;
    public AdBanner Banner { get; set; } = null!;
    public TutorialStep? Step { get; set; }
    public User? User { get; set; }
}

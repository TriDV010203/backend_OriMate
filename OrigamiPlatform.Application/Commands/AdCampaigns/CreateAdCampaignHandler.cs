using OrigamiPlatform.Application.DTOs.AdCampaigns;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.AdCampaigns;

public class CreateAdCampaignHandler
{
    private const int MaxNameLength = 200;
    private const decimal MinBudget = 100_000m;       // BV-30
    private const decimal MaxBudget = 100_000_000m;   // BV-30
    private const decimal MinCpm = 10_000m;           // BV-32
    private const decimal MaxCpm = 500_000m;          // BV-32
    private const decimal MinCpc = 500m;              // BV-33
    private const decimal MaxCpc = 50_000m;           // BV-33
    private const int MaxDurationDays = 90;           // BV-31

    private readonly IAdCampaignRepository _campaigns;
    private readonly INotificationService _notifications;
    private readonly IBlockedWordService _blockedWords;

    public CreateAdCampaignHandler(
        IAdCampaignRepository campaigns,
        INotificationService notifications,
        IBlockedWordService blockedWords)
        => (_campaigns, _notifications, _blockedWords) = (campaigns, notifications, blockedWords);

    public async Task<AdCampaignDto> HandleAsync(CreateAdCampaignCommand command, CancellationToken ct = default)
    {
        var req = command.Request;

        var name = (req.Name ?? string.Empty).Trim();
        if (name.Length is 0 or > MaxNameLength)
            throw new DomainException($"Campaign name must be between 1 and {MaxNameLength} characters.");

        if (await _blockedWords.ContainsBlockedWordAsync(name, ct))
            throw new DomainException("Campaign name contains a blocked word.");

        if (string.IsNullOrWhiteSpace(req.DestinationUrl)
            || !Uri.TryCreate(req.DestinationUrl, UriKind.Absolute, out _))
            throw new DomainException("A valid absolute destination URL is required.");

        if (string.IsNullOrWhiteSpace(req.BannerImageUrl))
            throw new DomainException("A banner image is required.");

        if (!Enum.TryParse<PricingType>(req.PricingType, ignoreCase: true, out var pricingType))
            throw new DomainException("Pricing type must be CPM or CPC.");

        // BV-32/BV-33: rate must be within the band for the pricing model.
        if (pricingType == PricingType.CPM && (req.RatePerUnit < MinCpm || req.RatePerUnit > MaxCpm))
            throw new DomainException($"CPM rate must be between {MinCpm:N0} and {MaxCpm:N0} VND per 1,000 impressions.");
        if (pricingType == PricingType.CPC && (req.RatePerUnit < MinCpc || req.RatePerUnit > MaxCpc))
            throw new DomainException($"CPC rate must be between {MinCpc:N0} and {MaxCpc:N0} VND per click.");

        // BV-30 / NAC-03: budget band.
        if (req.TotalBudget < MinBudget || req.TotalBudget > MaxBudget)
            throw new DomainException($"Budget must be between {MinBudget:N0} and {MaxBudget:N0} VND.");

        // BV-31 / NAC-02: start today or future, end after start, max 90 days.
        var today = DateTime.UtcNow.Date;
        if (req.StartDate.Date < today)
            throw new DomainException("Start date cannot be in the past.");
        if (req.EndDate.Date <= req.StartDate.Date)
            throw new DomainException("End date must be after the start date.");
        if ((req.EndDate.Date - req.StartDate.Date).TotalDays > MaxDurationDays)
            throw new DomainException($"Campaign duration cannot exceed {MaxDurationDays} days.");

        // Placement must exist and be active (configured by Admin).
        var placement = await _campaigns.GetActivePlacementAsync(req.PlacementId, ct)
            ?? throw new DomainException("The selected ad placement does not exist or is not active.");

        var now = DateTime.UtcNow;
        var campaignId = Guid.NewGuid();
        var campaign = new AdCampaign
        {
            Id = campaignId,
            PartnerId = command.PartnerId,
            PlacementId = placement.Id,
            Name = name,
            DestinationUrl = req.DestinationUrl.Trim(),
            PricingType = pricingType,
            RatePerUnit = req.RatePerUnit,
            TotalBudget = req.TotalBudget,
            BudgetRemaining = req.TotalBudget,
            Status = CampaignStatus.PendingApproval,   // AC-01
            StartDate = req.StartDate.Date,
            EndDate = req.EndDate.Date,
            CreatedAt = now,
            Banners =
            {
                new AdBanner
                {
                    Id = Guid.NewGuid(),
                    CampaignId = campaignId,
                    ImageUrl = req.BannerImageUrl.Trim(),
                    AltText = req.AltText?.Trim(),
                    CreatedAt = now
                }
            }
        };

        await _campaigns.AddAsync(campaign, ct);

        // AC-01: notify Managers that a campaign is pending review.
        await _notifications.NotifyUsersWithRoleAsync(
            UserRoleType.Manager,
            NotificationType.AdCampaignSubmitted,
            $"New ad campaign \"{name}\" is pending review.",
            "AdCampaign",
            campaignId,
            ct);

        var created = await _campaigns.GetByIdAsync(campaignId, ct)
            ?? throw new NotFoundException("Ad campaign not found after creation.");

        return created.ToDto();
    }
}

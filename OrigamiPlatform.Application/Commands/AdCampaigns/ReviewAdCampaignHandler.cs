using OrigamiPlatform.Application.DTOs.AdCampaigns;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.AdCampaigns;

public class ReviewAdCampaignHandler
{
    private readonly IAdCampaignRepository _campaigns;
    private readonly INotificationService _notifications;
    private readonly IAuditLogRepository _auditLog;

    public ReviewAdCampaignHandler(
        IAdCampaignRepository campaigns,
        INotificationService notifications,
        IAuditLogRepository auditLog)
        => (_campaigns, _notifications, _auditLog) = (campaigns, notifications, auditLog);

    public async Task<AdCampaignDto> HandleAsync(ReviewAdCampaignCommand command, CancellationToken ct = default)
    {
        var campaign = await _campaigns.GetByIdAsync(command.CampaignId, ct)
            ?? throw new NotFoundException("Ad campaign not found.");

        if (campaign.Status != CampaignStatus.PendingApproval)
            throw new DomainException("Only a campaign pending approval can be reviewed.");

        var now = DateTime.UtcNow;
        campaign.ApprovedBy = command.ManagerId;
        campaign.ApprovedAt = now;
        campaign.UpdatedAt = now;

        string message;
        string newValue;

        if (command.Approve)
        {
            // AC-02: approved campaigns go live on their scheduled start date.
            campaign.Status = CampaignStatus.Approved;
            campaign.RejectionReason = null;
            message = $"Your ad campaign \"{campaign.Name}\" has been approved.";
            newValue = "Approved";
        }
        else
        {
            var reason = (command.Reason ?? string.Empty).Trim();
            if (reason.Length == 0)
                throw new DomainException("A rejection reason is required.");

            campaign.Status = CampaignStatus.Rejected;
            campaign.RejectionReason = reason;
            message = $"Your ad campaign \"{campaign.Name}\" was rejected. Reason: {reason}";
            newValue = $"Rejected: {reason}";
        }

        await _campaigns.UpdateAsync(campaign, ct);

        // AC-03: notify the partner of the result.
        await _notifications.NotifyUserAsync(
            campaign.PartnerId,
            NotificationType.AdCampaignResult,
            message,
            "AdCampaign",
            campaign.Id,
            ct);

        // All approval actions are logged.
        await _auditLog.LogAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = command.ManagerId,
            Action = command.Approve ? "ApproveAdCampaign" : "RejectAdCampaign",
            EntityType = "AdCampaign",
            EntityId = campaign.Id.ToString(),
            OldValue = CampaignStatus.PendingApproval.ToString(),
            NewValue = newValue,
            CreatedAt = now
        }, ct);

        return campaign.ToDto();
    }
}

using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Common;

// FT-35: shared badge-award helper, mirrors HatGapAwardService's role for the Hạt Gấp ledger.
// Called from existing trigger points (CreateAchievementHandler, CompleteTutorialStepHandler)
// and the new DailyChallenge handlers — never fails the caller's main flow.
public class BadgeAwardService
{
    private readonly IBadgeRepository _badges;
    private readonly IUserBadgeRepository _userBadges;
    private readonly INotificationService _notifications;

    public BadgeAwardService(IBadgeRepository badges, IUserBadgeRepository userBadges, INotificationService notifications)
        => (_badges, _userBadges, _notifications) = (badges, userBadges, notifications);

    /// <summary>No-ops silently if the badge code doesn't exist/isn't active, or the user already has it — safe to call unconditionally.</summary>
    public async Task TryAwardAsync(Guid userId, string badgeCode, Guid? contextRefId = null, CancellationToken ct = default)
    {
        try
        {
            var badge = await _badges.GetByCodeAsync(badgeCode, ct);
            if (badge is null || !badge.IsActive)
                return;

            if (await _userBadges.ExistsAsync(userId, badge.Id, ct))
                return;

            await _userBadges.AddAsync(new UserBadge
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                BadgeId = badge.Id,
                EarnedAt = DateTime.UtcNow,
                ContextRefId = contextRefId
            }, ct);

            await _notifications.NotifyUserAsync(
                userId: userId,
                type: NotificationType.BadgeEarned,
                message: $"Bạn vừa đạt danh hiệu {badge.IconEmoji} \"{badge.Name}\"!",
                entityType: nameof(Badge),
                entityId: badge.Id,
                ct: ct);
        }
        catch
        {
            // badge award failure must not affect the caller's main flow
        }
    }
}

using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Follows;

public class ToggleFollowHandler
{
    private readonly IFollowRepository _follows;
    private readonly INotificationService _notifications;

    public ToggleFollowHandler(IFollowRepository follows, INotificationService notifications)
    {
        _follows = follows;
        _notifications = notifications;
    }

    public async Task<bool> HandleAsync(ToggleFollowCommand cmd, CancellationToken ct = default)
    {
        if (cmd.FollowerId == cmd.FollowingId)
        {
            throw new DomainException("You cannot follow yourself.");
        }

        var existing = await _follows.GetFollowAsync(cmd.FollowerId, cmd.FollowingId, ct);

        if (existing != null)
        {
            await _follows.RemoveAsync(existing, ct);
            return false;
        }
        else
        {
            var follow = new FollowRelationship
            {
                FollowerId = cmd.FollowerId,
                FollowingId = cmd.FollowingId,
                CreatedAt = DateTime.UtcNow
            };
            await _follows.AddAsync(follow, ct);

            // Notifications
            await _notifications.NotifyUserAsync(
                userId: cmd.FollowingId,
                type: NotificationType.NewFollower,
                message: "Bạn có người theo dõi mới.",
                entityType: "User",
                entityId: cmd.FollowerId,
                ct: ct
            );

            return true;
        }
    }
}
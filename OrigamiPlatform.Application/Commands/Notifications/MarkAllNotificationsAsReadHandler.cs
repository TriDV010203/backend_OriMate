using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Commands.Notifications;

public class MarkAllNotificationsAsReadHandler
{
    private readonly INotificationRepository _notificationRepository;

    public MarkAllNotificationsAsReadHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task HandleAsync(MarkAllNotificationsAsReadCommand cmd, CancellationToken ct = default)
    {
        await _notificationRepository.MarkAllAsReadAsync(cmd.UserId, ct);
    }
}
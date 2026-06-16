using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Notifications;

public class MarkNotificationAsReadHandler
{
    private readonly INotificationRepository _notificationRepository;

    public MarkNotificationAsReadHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task HandleAsync(MarkNotificationAsReadCommand cmd, CancellationToken ct = default)
    {
        var notification = await _notificationRepository.GetByIdAsync(cmd.NotificationId, ct);

        // Kiểm tra xem thông báo có tồn tại và có thuộc về đúng User này không
        if (notification == null || notification.RecipientId != cmd.UserId)
        {
            throw new NotFoundException("Notification not found.");
        }

        notification.IsRead = true;
        await _notificationRepository.UpdateAsync(notification, ct);
    }
}
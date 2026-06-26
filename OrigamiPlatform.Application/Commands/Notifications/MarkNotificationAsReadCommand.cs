namespace OrigamiPlatform.Application.Commands.Notifications;

public record MarkNotificationAsReadCommand(Guid NotificationId, Guid UserId);
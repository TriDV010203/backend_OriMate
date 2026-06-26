using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Interfaces;

public interface INotificationService
{
    Task NotifyUsersWithRoleAsync(
        UserRoleType role,
        NotificationType type,
        string message,
        string entityType,
        Guid entityId,
        CancellationToken ct = default);

    Task NotifyUserAsync(
        Guid userId,
        NotificationType type,
        string message,
        string entityType,
        Guid entityId,
        CancellationToken ct = default);
}

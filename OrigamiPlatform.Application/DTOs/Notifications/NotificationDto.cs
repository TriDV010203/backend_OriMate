using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.DTOs.Notifications;

public record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Message,
    string? EntityType,
    Guid? EntityId,
    bool IsRead,
    DateTime CreatedAt
);
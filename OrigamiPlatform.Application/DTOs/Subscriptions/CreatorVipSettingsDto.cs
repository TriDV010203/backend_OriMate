namespace OrigamiPlatform.Application.DTOs.Subscriptions;

public record CreatorVipSettingsDto(
    Guid Id,
    Guid CreatorId,
    decimal Price,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

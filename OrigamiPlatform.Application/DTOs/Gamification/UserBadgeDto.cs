namespace OrigamiPlatform.Application.DTOs.Gamification;

public record UserBadgeDto(
    Guid BadgeId,
    string Code,
    string Name,
    string Description,
    string IconEmoji,
    string Category,
    DateTime EarnedAt
);

namespace OrigamiPlatform.Application.DTOs.Gamification;

public record BadgeDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string IconEmoji,
    string Category,
    int? Threshold
);

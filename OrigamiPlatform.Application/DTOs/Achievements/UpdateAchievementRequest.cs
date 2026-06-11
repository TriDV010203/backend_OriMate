namespace OrigamiPlatform.Application.DTOs.Achievements;

public record UpdateAchievementRequest(
    string? PhotoUrl,
    string? Note,
    bool IsPublic
);

namespace OrigamiPlatform.Application.DTOs.Achievements;

public record CreateAchievementRequest(
    Guid TutorialId,
    string? PhotoUrl,
    string? Note,
    bool IsPublic = true
);

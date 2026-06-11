namespace OrigamiPlatform.Application.DTOs.Achievements;

public record AchievementDto(
    Guid Id,
    Guid UserId,
    Guid TutorialId,
    string TutorialTitle,
    string TutorialSlug,
    string? PhotoUrl,
    string? Note,
    bool IsPublic,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

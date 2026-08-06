namespace OrigamiPlatform.Application.DTOs.LearningPathModes;

public record ModeUnlockSubmissionDto(
    Guid Id,
    Guid UserId,
    string UserDisplayName,
    string? UserAvatarUrl,
    Guid LearningPathModeId,
    string LearningPathModeName,
    Guid TutorialId,
    string TutorialTitle,
    string PhotoUrl,
    string? Note,
    string Status,
    string? ReviewNote,
    DateTime CreatedAt,
    DateTime? ReviewedAt
);

namespace OrigamiPlatform.Application.DTOs.LearningPathModes;

public record LearningPathModeAdminDto(
    Guid Id,
    string Name,
    string? Description,
    int SortOrder,
    bool IsActive,
    int PathCount,
    Guid? UnlockTestTutorialId,
    string? UnlockTestTutorialTitle,
    string? UnlockTestInstructions,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

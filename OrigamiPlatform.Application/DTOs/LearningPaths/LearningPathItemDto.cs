namespace OrigamiPlatform.Application.DTOs.LearningPaths;

public record LearningPathItemDto(
    int ItemOrder,
    Guid TutorialId,
    string TutorialTitle,
    string TutorialSlug,
    string? TutorialCoverImageUrl,
    string TutorialDifficulty,
    int CategoryId,
    string CategoryName
);

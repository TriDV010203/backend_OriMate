namespace OrigamiPlatform.Application.DTOs.LearningPaths;

public record CreateLearningPathRequest(
    Guid LearningPathModeId,
    string Title,
    string Description,
    string? CoverImageUrl,
    List<Guid> TutorialIds
);

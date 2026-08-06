namespace OrigamiPlatform.Application.DTOs.LearningPaths;

public record UpdateLearningPathRequest(
    Guid LearningPathModeId,
    string Title,
    string Description,
    string? CoverImageUrl,
    List<Guid> TutorialIds
);

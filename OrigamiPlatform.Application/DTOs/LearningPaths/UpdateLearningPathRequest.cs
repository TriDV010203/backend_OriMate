namespace OrigamiPlatform.Application.DTOs.LearningPaths;

public record UpdateLearningPathRequest(
    string Title,
    string Description,
    string? CoverImageUrl,
    List<Guid> TutorialIds
);

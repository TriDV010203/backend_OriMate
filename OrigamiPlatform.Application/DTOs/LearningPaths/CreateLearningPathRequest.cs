namespace OrigamiPlatform.Application.DTOs.LearningPaths;

public record CreateLearningPathRequest(
    string Title,
    string Description,
    string? CoverImageUrl,
    List<Guid> TutorialIds
);

namespace OrigamiPlatform.Application.DTOs.LearningPaths;

public record LearningPathDto(
    Guid Id,
    Guid LearningPathModeId,
    string LearningPathModeName,
    string Title,
    string Description,
    string? CoverImageUrl,
    string Status,
    List<LearningPathItemDto> Items,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? PublishedAt
);

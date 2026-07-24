namespace OrigamiPlatform.Application.DTOs.LearningPaths;

public record LearningPathListItemDto(
    Guid Id,
    string Title,
    string Description,
    string? CoverImageUrl,
    string Status,
    int ItemCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? PublishedAt
);

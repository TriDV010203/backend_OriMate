namespace OrigamiPlatform.Application.DTOs.Tutorials;

public record TutorialListItemDto(
    Guid Id,
    string Title,
    string Slug,
    string Description,
    string? CoverImageUrl,
    string Type,
    string? Difficulty,
    int CategoryId,
    string CategoryName,
    AuthorDto Author,
    int StepCount,
    DateTime PublishedAt,
    bool IsVipLocked = false
);

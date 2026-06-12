namespace OrigamiPlatform.Application.DTOs.Journals;

public record JournalDto(
    Guid Id,
    Guid UserId,
    Guid? LinkedTutorialId,
    string? LinkedTutorialTitle,
    string? LinkedTutorialSlug,
    string Content,
    string? ImageUrls,
    bool IsPublic,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

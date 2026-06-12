namespace OrigamiPlatform.Application.DTOs.Journals;

public record UpdateJournalRequest(
    Guid? LinkedTutorialId,
    string Content,
    string? ImageUrls,
    bool IsPublic
);

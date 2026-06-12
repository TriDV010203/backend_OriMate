namespace OrigamiPlatform.Application.DTOs.Journals;

public record CreateJournalRequest(
    Guid? LinkedTutorialId,
    string Content,
    string? ImageUrls,
    bool IsPublic = true
);

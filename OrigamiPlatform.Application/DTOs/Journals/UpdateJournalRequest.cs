namespace OrigamiPlatform.Application.DTOs.Journals;

public record UpdateJournalRequest(
    Guid? LinkedTutorialId,
    string Content,
    List<string>? ImageUrls,
    bool IsPublic
);

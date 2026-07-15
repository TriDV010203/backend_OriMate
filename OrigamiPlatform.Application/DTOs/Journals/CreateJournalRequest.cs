namespace OrigamiPlatform.Application.DTOs.Journals;

public record CreateJournalRequest(
    Guid? LinkedTutorialId,
    string Content,
    List<string>? ImageUrls = null,
    bool IsPublic = false
);

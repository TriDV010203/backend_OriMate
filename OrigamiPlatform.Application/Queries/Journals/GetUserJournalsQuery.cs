namespace OrigamiPlatform.Application.Queries.Journals;

public record GetUserJournalsQuery(
    Guid UserId,
    Guid? CurrentUserId,
    int Page,
    int PageSize);

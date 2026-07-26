namespace OrigamiPlatform.Application.Queries.DailyChallenge;

public record GetChallengeSubmissionsQuery(
    DateOnly ChallengeDate,
    int Page,
    int PageSize,
    Guid? CurrentUserId
);

namespace OrigamiPlatform.Application.Queries.WeeklyChallenge;

public record GetWeeklyChallengeSubmissionsQuery(
    DateOnly ChallengeDate,
    int Page,
    int PageSize,
    Guid? CurrentUserId
);

using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Queries.WeeklyChallenge;

public record GetAdminWeeklyChallengeCalendarQuery(
    DateOnly? FromDate,
    DateOnly? ToDate,
    DailyChallengeStatus? Status,
    int Page = 1,
    int PageSize = 31
);

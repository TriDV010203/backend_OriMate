using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Queries.DailyChallenge;

public record GetAdminChallengeCalendarQuery(
    DateOnly? FromDate,
    DateOnly? ToDate,
    DailyChallengeStatus? Status,
    int Page = 1,
    int PageSize = 31
);

namespace OrigamiPlatform.Application.Queries.Achievements;

public record GetUserAchievementsQuery(
    Guid UserId,
    Guid? CurrentUserId,
    int Page,
    int PageSize);

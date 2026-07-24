namespace OrigamiPlatform.Application.Queries.Users;

public record GetFollowingQuery(Guid TargetUserId, Guid? CurrentUserId, int Page, int PageSize);

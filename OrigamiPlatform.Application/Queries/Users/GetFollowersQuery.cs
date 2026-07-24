namespace OrigamiPlatform.Application.Queries.Users;

public record GetFollowersQuery(Guid TargetUserId, Guid? CurrentUserId, int Page, int PageSize);

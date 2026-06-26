namespace OrigamiPlatform.Application.Queries.Users;

public record GetCreatorProfileQuery(Guid TargetUserId, Guid? CurrentUserId);
namespace OrigamiPlatform.Application.Queries.Users;

public record GetFeaturedCreatorsQuery(int Count, Guid? CurrentUserId);

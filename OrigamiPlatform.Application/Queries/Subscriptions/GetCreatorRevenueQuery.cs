namespace OrigamiPlatform.Application.Queries.Subscriptions;

public record GetCreatorRevenueQuery(Guid CreatorId, Guid RequestingUserId);

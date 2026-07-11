namespace OrigamiPlatform.Application.Queries.Subscriptions;

public record GetMySubscriptionsQuery(Guid SubscriberId, int Page, int PageSize);

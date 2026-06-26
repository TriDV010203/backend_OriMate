namespace OrigamiPlatform.Application.Queries.Notifications;

public record GetNotificationsQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 10
);